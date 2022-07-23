import glob
import os
import sys
from datetime import datetime

try:
    sys.path.append(glob.glob('../carla/dist/carla-*%d.%d-%s.egg' % (
        sys.version_info.major,
        sys.version_info.minor,
        'win-amd64' if os.name == 'nt' else 'linux-x86_64'))[0])
except IndexError:
    pass

import cv2
import yaml
import time
import carla
import numpy as np
from PIL import Image
from queue import Queue, Empty


num_frames = 50
num_robots = 6
num_pucks = 16

# HEV camera params
bev_z = 750
bev_fov = '17.5'

folder = datetime.now().strftime("%m-%d-%Y_%H-%M-%S")
print(f'outputting to: {folder}')
os.mkdir(f'_out/{folder}/')
os.mkdir(f'_out/{folder}/hev')
os.mkdir(f'_out/{folder}/data')
for i in range(num_robots):
    os.mkdir(f'_out/{folder}/robot_{i}')


def get_cam_intrinsics(bp):
    """ 
    Build the K projection matrix.

    K = [[Fx,  0, image_w/2],
         [ 0, Fy, image_h/2],
         [ 0,  0,         1]]
    """
    image_w = bp.get_attribute("image_size_x").as_int()
    image_h = bp.get_attribute("image_size_y").as_int()
    fov = bp.get_attribute("fov").as_float()
    focal_x = image_w / (2.0 * np.tan(fov * np.pi / 360.0))
    focal_y = image_h / (2.0 * np.tan(fov * np.pi / 360.0))

    K = np.identity(3)
    K[0, 0] = focal_x
    K[1, 1] = focal_y
    K[0, 2] = image_w / 2.0
    K[1, 2] = image_h / 2.0

    return K


class Robot:
    """Robot camera and body actors + thread safe queue for cam imgs."""

    def __init__(self, world, spawn):
        """Create Robot."""
        bp_lib = world.get_blueprint_library()

        self.robot = world.spawn_actor(
            blueprint=bp_lib.find('walker.pedestrian.0050'),
            transform=spawn)

        cam_bp = bp_lib.find('sensor.camera.rgb')
        cam_bp.set_attribute('image_size_x', '480')
        cam_bp.set_attribute('image_size_y', '224')
        self.camera = world.spawn_actor(
            blueprint=cam_bp,
            transform=carla.Transform(
                carla.Location(x=3.4, z=0.9),
                carla.Rotation(pitch=-20)),
            attach_to=self.robot)

        self.queue = Queue()
        self.camera.listen(self.queue.put)
        
        self.intrinsics = get_cam_intrinsics(cam_bp)

    def random_transform(self):
        self.robot.set_transform(carla.Transform(
            carla.Location(x=np.random.uniform(-70, 70), y=np.random.uniform(-90, 90), z=0),
            carla.Rotation(yaw=np.random.uniform(0, 360)),
        ))


def random_transform():
    return carla.Transform(
        carla.Location(x=np.random.uniform(-70, 70), y=np.random.uniform(-90, 90), z=1.15),
        carla.Rotation(yaw=np.random.uniform(0, 360)),
    )


def scan(client):
    """
    Record training data for a Herds Eye View.

    Record the Herds Eye View (bev), Semantic Segmentation (seg), and base
    camera (rgb) trio on the exact same frame, then save and randomly move
    through x-y limints and z-angle (0-360) to record taining data
    """
    world = client.get_world()
    file = datetime.now().strftime("%m-%d-%Y_%H-%M-%S")

    bp_lib = world.get_blueprint_library()
    original_settings = world.get_settings()
    settings = world.get_settings()
    settings.synchronous_mode = True
    settings.fixed_delta_seconds = 0.001
    world.apply_settings(settings)

    bev_camera = None
    robots = []
    pucks = []

    try:
        for _ in range(num_robots):
            robots.append(Robot(world, random_transform()))

        for _ in range(num_pucks):
            pucks.append(world.spawn_actor(
                blueprint=bp_lib.find("static.prop.slide"),
                transform=random_transform()))

        bev_camera_bp = bp_lib.find("sensor.camera.semantic_segmentation")
        bev_camera_bp.set_attribute('image_size_x', '200')
        bev_camera_bp.set_attribute('image_size_y', '200')
        bev_camera_bp.set_attribute('fov', bev_fov)

        bev_camera = world.spawn_actor(
            blueprint=bev_camera_bp,
            transform=carla.Transform(
                carla.Location(x=0, y=0, z=bev_z),
                carla.Rotation(pitch=-90.0)))

        # The sensor data will be saved in thread-safe Queues
        bev_queue = Queue()
        bev_camera.listen(bev_queue.put)

        # print((((start_x, start_y), (end_x, end_y)), centroid_dir))
        for frame in range(num_frames):
            # move robots & pucks to new random locations
            for robot in robots:
                robot.random_transform()
            for puck in pucks:
                puck.set_transform(random_transform())

            world.tick()
            world_frame = world.get_snapshot().frame

            try:
                # Get the data from latest tick
                bev_data = bev_queue.get(True, 1.0)
                for robot in robots:
                    robot.data = robot.queue.get(True, 1.0)
            except Empty:
                print(f"[Warning] Some sensor data has been missed, frame: {frame, world_frame}")
                continue
            if not (bev_data.frame == robots[0].data.frame == robots[-1].data.frame == world_frame):
                print('[Warning] Frame camera frame miss match, skipping this frame')
                print(bev_data.frame, robots[0].data.frame, robots[-1].data.frame, world_frame)
                continue

            # confirmed synchronized information from all cameras
            assert bev_data.frame == robots[0].data.frame == robots[-1].data.frame == world_frame
            sys.stdout.write(f"\rFrame: {frame+1}/{num_frames}")
            sys.stdout.flush()

            bev_data.convert(carla.ColorConverter.CityScapesPalette)
            bev_array = np.frombuffer(bev_data.raw_data, dtype=np.dtype("uint8"))
            bev_array = np.reshape(bev_array, (bev_data.height, bev_data.width, 4))
            bev_array = bev_array[:, :, :3][:, :, ::-1]
            bev_image = Image.fromarray(bev_array)
            bev_image.save(f"_out/{folder}/hev/{file}__{frame}.png")

            intrinsics = list()
            extrinsics = list()

            for i, robot in enumerate(robots):
                robot_array = np.frombuffer(robot.data.raw_data, dtype=np.dtype("uint8"))
                robot_array = np.reshape(robot_array, (robot.data.height, robot.data.width, 4))
                robot_array = robot_array[:, :, :3][:, :, ::-1]
                robot_image = Image.fromarray(robot_array)
                robot_image.save(f"_out/{folder}/robot_{i}/{file}__{frame}.png")

                extrinsics.append(robot.camera.get_transform().get_matrix())
                intrinsics.append(robot.intrinsics)
            
            data = {
                'intrinsics': np.array(intrinsics),
                'extrinsics': np.array(extrinsics),
            }
            np.savez_compressed(f'_out/{folder}/data/{file}__{frame}.npz', aux=data)
            
    finally:
        world.apply_settings(original_settings)

        # Destroy cameras so they're not left hanging in sim
        if bev_camera:
            bev_camera.destroy()
        if robots:
            for robot in robots:
                robot.robot.destroy()
                robot.camera.destroy()
        if pucks:
            for puck in pucks:
                puck.destroy()


if __name__ == '__main__':
    try:
        client = carla.Client('127.0.0.1', 2000)
        client.set_timeout(5.0)
        scan(client)

    except KeyboardInterrupt:
        print('\nCancelled by user. Bye!')
