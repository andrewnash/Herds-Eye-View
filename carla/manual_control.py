#!/usr/bin/env python
import glob
import os
import sys

try:
    sys.path.append(glob.glob('../carla/dist/carla-*%d.%d-%s.egg' % (
        sys.version_info.major,
        sys.version_info.minor,
        'win-amd64' if os.name == 'nt' else 'linux-x86_64'))[0])
except IndexError:
    pass

import carla
import argparse
import random
import cv2
import time
import yaml
import numpy as np
from carla import ColorConverter as cc
try:
    import pygame
    from pygame.locals import KMOD_CTRL
    from pygame.locals import KMOD_SHIFT
    from pygame.locals import K_0
    from pygame.locals import K_9
    from pygame.locals import K_BACKQUOTE
    from pygame.locals import K_BACKSPACE
    from pygame.locals import K_COMMA
    from pygame.locals import K_DOWN
    from pygame.locals import K_ESCAPE
    from pygame.locals import K_F1
    from pygame.locals import K_LEFT
    from pygame.locals import K_PERIOD
    from pygame.locals import K_RIGHT
    from pygame.locals import K_SLASH
    from pygame.locals import K_SPACE
    from pygame.locals import K_TAB
    from pygame.locals import K_UP
    from pygame.locals import K_a
    from pygame.locals import K_b
    from pygame.locals import K_c
    from pygame.locals import K_d
    from pygame.locals import K_g
    from pygame.locals import K_h
    from pygame.locals import K_i
    from pygame.locals import K_j  # igvc
    from pygame.locals import K_k  # igvc
    from pygame.locals import K_l
    from pygame.locals import K_m
    from pygame.locals import K_n
    from pygame.locals import K_o
    from pygame.locals import K_p
    from pygame.locals import K_q
    from pygame.locals import K_r
    from pygame.locals import K_s
    from pygame.locals import K_t
    from pygame.locals import K_u  # igvc
    from pygame.locals import K_v
    from pygame.locals import K_w
    from pygame.locals import K_x
    from pygame.locals import K_z
    from pygame.locals import K_MINUS
    from pygame.locals import K_EQUALS
except ImportError:
    raise RuntimeError(
        'cannot import pygame, make sure pygame package is installed')

save_imgs = False
# ==============================================================================
# -- KeyboardControl -----------------------------------------------------------
# ==============================================================================

class KeyboardControl(object):
    """Class that handles keyboard input."""

    def __init__(self, world, start_in_autopilot):
        self._autopilot_enabled = start_in_autopilot
        # if isinstance(world.player, carla.Vehicle):
        self._control = carla.VehicleControl()
        self._lights = carla.VehicleLightState.NONE
        # world.player.set_autopilot(self._autopilot_enabled)
        # world.player.set_light_state(self._lights)
        if isinstance(world.player, carla.Walker):
            self._control = carla.WalkerControl()
            self._autopilot_enabled = False
            self._rotation = world.player.get_transform().rotation
        else:
            raise NotImplementedError("Actor type not supported")
        self._steer_cache = 0.0
        #world.hud.notification("Press 'H' or '?' for help.", seconds=4.0)

    def parse_events(self, client, world, clock, sync_mode):
        if isinstance(self._control, carla.VehicleControl):
            current_lights = self._lights
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                return True
            elif event.type == pygame.KEYUP:
                if self._is_quit_shortcut(event.key):
                    return True
                elif event.key == K_BACKSPACE:
                    world.restart()
                # elif event.key == K_c and pygame.key.get_mods() & KMOD_SHIFT:
                #     world.next_weather(reverse=True)
                # elif event.key == K_c:
                #     world.next_weather()
                elif event.key == K_r and not (pygame.key.get_mods() & KMOD_CTRL):
                    world.camera_manager.toggle_recording()
                elif event.key == K_p:
                    save_imgs = not save_imges
                # elif event.key == K_p and (pygame.key.get_mods() & KMOD_CTRL):
                #     # stop recorder
                #     client.stop_recorder()
                #     world.recording_enabled = False
                #     # work around to fix camera at start of replaying
                #     current_index = world.camera_manager.index
                #     world.destroy_sensors()
                #     # disable autopilot
                #     self._autopilot_enabled = False
                #     world.player.set_autopilot(self._autopilot_enabled)
                #     world.hud.notification("Replaying file 'manual_recording.rec'")
                #     # replayer
                #     client.replay_file("manual_recording.rec", world.recording_start, 0, 0)
                #     world.camera_manager.set_sensor(current_index)
                elif event.key == K_MINUS and (pygame.key.get_mods() & KMOD_CTRL):
                    if pygame.key.get_mods() & KMOD_SHIFT:
                        world.recording_start -= 10
                    else:
                        world.recording_start -= 1
                    world.hud.notification(
                        "Recording start time is %d" % (world.recording_start))
                elif event.key == K_EQUALS and (pygame.key.get_mods() & KMOD_CTRL):
                    if pygame.key.get_mods() & KMOD_SHIFT:
                        world.recording_start += 10
                    else:
                        world.recording_start += 1
                    world.hud.notification(
                        "Recording start time is %d" % (world.recording_start))

        # self._parse_vehicle_keys(pygame.key.get_pressed(), clock.get_time())
        self._parse_walker_keys(pygame.key.get_pressed(),
                                clock.get_time(), world)
        # self._control.reverse = self._control.gear < 0
        # # Set automatic control-related vehicle lights
        # if self._control.brake:
        #     current_lights |= carla.VehicleLightState.Brake
        # else: # Remove the Brake flag
        #     current_lights &= ~carla.VehicleLightState.Brake
        # if self._control.reverse:
        #     current_lights |= carla.VehicleLightState.Reverse
        # else: # Remove the Reverse flag
        #     current_lights &= ~carla.VehicleLightState.Reverse
        # if current_lights != self._lights: # Change the light state only if necessary
        #     self._lights = current_lights
        #     world.player.set_light_state(carla.VehicleLightState(self._lights))

        world.player.apply_control(self._control)

    def _parse_vehicle_keys(self, keys, milliseconds):
        if keys[K_UP] or keys[K_w]:
            self._control.throttle = min(self._control.throttle + 0.01, 1.00)
        else:
            self._control.throttle = 0.0

        if keys[K_DOWN] or keys[K_s]:
            self._control.brake = min(self._control.brake + 0.2, 1)
        else:
            self._control.brake = 0

        steer_increment = 5e-4 * milliseconds
        if keys[K_LEFT] or keys[K_a]:
            if self._steer_cache > 0:
                self._steer_cache = 0
            else:
                self._steer_cache -= steer_increment
        elif keys[K_RIGHT] or keys[K_d]:
            if self._steer_cache < 0:
                self._steer_cache = 0
            else:
                self._steer_cache += steer_increment
        else:
            self._steer_cache = 0.0
        self._steer_cache = min(0.7, max(-0.7, self._steer_cache))
        self._control.steer = round(self._steer_cache, 1)
        self._control.hand_brake = keys[K_SPACE]

    def _parse_walker_keys(self, keys, milliseconds, world):
        self._control.speed = 0.0
        if keys[K_DOWN] or keys[K_s]:
            self._control.speed = 0.0
        if keys[K_LEFT] or keys[K_a]:
            self._control.speed = .01
            self._rotation.yaw -= 0.08 * milliseconds
        if keys[K_RIGHT] or keys[K_d]:
            self._control.speed = .01
            self._rotation.yaw += 0.08 * milliseconds
        if keys[K_UP] or keys[K_w]:
            self._control.speed = world.player_max_speed_fast if pygame.key.get_mods(
            ) & KMOD_SHIFT else world.player_max_speed
        self._control.jump = keys[K_SPACE]
        self._rotation.yaw = round(self._rotation.yaw, 1)
        self._control.direction = self._rotation.get_forward_vector()

    @staticmethod
    def _is_quit_shortcut(key):
        return (key == K_ESCAPE) or (key == K_q and pygame.key.get_mods() & KMOD_CTRL)

# ---


class CustomTimer:
    def __init__(self):
        try:
            self.timer = time.perf_counter
        except AttributeError:
            self.timer = time.time

    def time(self):
        return self.timer()


class DisplayManager:
    def __init__(self, grid_size, window_size):
        pygame.init()
        pygame.font.init()
        self.display = pygame.display.set_mode(
            window_size, pygame.HWSURFACE | pygame.DOUBLEBUF)

        self.grid_size = grid_size
        self.window_size = window_size
        self.sensor_list = []

    def get_window_size(self):
        return [int(self.window_size[0]), int(self.window_size[1])]

    def get_display_size(self):
        return [int(self.window_size[0]/self.grid_size[1]), int(self.window_size[1]/self.grid_size[0])]

    def get_display_offset(self, gridPos):
        dis_size = self.get_display_size()
        return [int(gridPos[1] * dis_size[0]), int(gridPos[0] * dis_size[1])]

    def add_sensor(self, sensor):
        self.sensor_list.append(sensor)

    def get_sensor_list(self):
        return self.sensor_list

    def render(self):
        if not self.render_enabled():
            return

        for s in self.sensor_list:
            s.render()

        pygame.display.flip()

    def destroy(self):
        for s in self.sensor_list:
            s.destroy()

    def render_enabled(self):
        return self.display != None


class SensorManager:
    def __init__(self, world, display_man, sensor_type, transform, attached, sensor_options, display_pos):
        self.surface = None
        self.world = world
        self.display_man = display_man
        self.display_pos = display_pos
        self.sensor = self.init_sensor(
            sensor_type, transform, attached, sensor_options)
        self.sensor_options = sensor_options
        self.timer = CustomTimer()

        self.time_processing = 0.0
        self.tics_processing = 0

        self.display_man.add_sensor(self)

    def init_sensor(self, sensor_type, transform, attached, sensor_options):
        if sensor_type == 'RGBCamera':
            camera_bp = self.world.get_blueprint_library().find('sensor.camera.rgb')
            disp_size = self.display_man.get_display_size()
            camera_bp.set_attribute('image_size_x', str(disp_size[0]))
            camera_bp.set_attribute('image_size_y', str(disp_size[1]))

            for key in sensor_options:
                camera_bp.set_attribute(key, sensor_options[key])

            camera = self.world.spawn_actor(
                camera_bp, transform, attach_to=attached)
            camera.listen(self.save_rgb_image)

            return camera

        if sensor_type == 'SemanticRGBCamera':
            camera_bp = self.world.get_blueprint_library().find(
                'sensor.camera.instance_segmentation')
            disp_size = self.display_man.get_display_size()

            camera_bp.set_attribute('image_size_x', str(disp_size[0]))
            camera_bp.set_attribute('image_size_y', str(disp_size[1]))
            camera_bp.set_attribute('fov', '17.5')

            for key in sensor_options:
                camera_bp.set_attribute(key, sensor_options[key])

            camera = self.world.spawn_actor(
                camera_bp, transform, attach_to=attached)
            camera.listen(self.save_semantic_image)

            return camera

    def get_sensor(self):
        return self.sensor

    def save_rgb_image(self, image):
        t_start = self.timer.time()

        image.convert(carla.ColorConverter.Raw)
        array = np.frombuffer(image.raw_data, dtype=np.dtype("uint8"))
        array = np.reshape(array, (image.height, image.width, 4))
        array = array[:, :, :3]
        array = array[:, :, ::-1]

        if self.display_man.render_enabled():
            self.surface = pygame.surfarray.make_surface(array.swapaxes(0, 1))

        if save_imgs:
                img = Image.fromarray(rgb_array)
                img.save(f"_out/{folder}/rgb/{file}__{frame}.png")

        t_end = self.timer.time()
        self.time_processing += (t_end-t_start)
        self.tics_processing += 1

    def save_semantic_image(self, image):
        t_start = self.timer.time()

        image.convert(carla.ColorConverter.CityScapesPalette)
        array = np.frombuffer(image.raw_data, dtype=np.dtype("uint8"))
        array = np.reshape(array, (image.height, image.width, 4))
        array = array[:, :, :3]
        array = array[:, :, ::-1]

        if save_imgs:
                img = Image.fromarray(rgb_array)
                img.save(f"_out/{folder}/rgb/{file}__{frame}.png")

        if self.display_man.render_enabled():
            self.surface = pygame.surfarray.make_surface(array.swapaxes(0, 1))

        t_end = self.timer.time()
        self.time_processing += (t_end-t_start)
        self.tics_processing += 1

    def render(self):
        if self.surface is not None:
            offset = self.display_man.get_display_offset(self.display_pos)
            self.display_man.display.blit(self.surface, offset)

    def destroy(self):
        self.sensor.destroy()


def run_simulation(args, client):
    """This function performed one test run using the args parameters
    and connecting to the carla client passed.
    """

    display_manager = None
    vehicle = None
    vehicle_list = []
    timer = CustomTimer()



    try:

        # Getting the world and
        world = client.get_world()
        original_settings = world.get_settings()

        # if args.sync:
        #     traffic_manager = client.get_trafficmanager(8000)
        #     settings = world.get_settings()
        #     traffic_manager.set_synchronous_mode(True)
        #     settings.synchronous_mode = True
        #     settings.fixed_delta_seconds = 0.05
        #     world.apply_settings(settings)

        # Instanciating the vehicle to which we attached the sensors
        bp = world.get_blueprint_library().filter('walker.pedestrian.0050')[0]
        #print(world.get_blueprint_library().filter('static.prop.*'))

        # blueprints = [bp for bp in world.get_blueprint_library().filter('static.prop.BotsPuck')]
        # for blueprint in blueprints:
        #    print(blueprint.id)
        #    for attr in blueprint:
        #        print('  - {}'.format(attr))

        spawn_point = carla.Transform(
            carla.Location(x=-33.4, y=-47.40, z=1.0), carla.Rotation(yaw=90))
        vehicle = world.spawn_actor(bp, spawn_point)
        vehicle_list.append(vehicle)
        world.player = vehicle
        world.player_max_speed = 1.589
        world.player_max_speed_fast = 3.713
        # vehicle.set_autopilot(True)
        cam_z = 1.43
        pitch = -30

        # Display Manager organize all the sensors an its display in a window
        # If can easily configure the grid and the total window size
        display_manager = DisplayManager(grid_size=[2, 2], window_size=[
                                         args.width, args.height])

        # Then, SensorManager can be used to spawn RGBCamera & SemanticCamera as needed
        # and assign each of them to a grid position,
        SensorManager(world, display_manager, 'RGBCamera', carla.Transform(carla.Location(x=3.4, z=0.9), carla.Rotation(pitch=-20)),
                      vehicle, {}, display_pos=[0, 0])
        SensorManager(world, display_manager, 'RGBCamera', carla.Transform(carla.Location(x=-10, z=6), carla.Rotation(pitch=-20)),
                      vehicle, {}, display_pos=[0, 1])
        SensorManager(world, display_manager, 'SemanticRGBCamera', carla.Transform(carla.Location(x=0, z=1150), carla.Rotation(pitch=-90)),
                      None, {}, display_pos=[1, 0])
        # SensorManager(world, display_manager, 'SemanticRGBCamera', carla.Transform(carla.Location(x=5, z=cam_z+12), carla.Rotation(yaw=0, pitch=-90)),
        #               vehicle, {}, display_pos=[1, 1])
        controller = KeyboardControl(world, False)

        # Simulation loop
        call_exit = False
        time_init_sim = timer.time()
        clock = pygame.time.Clock()
        while True:
            # Carla Tick
            if args.sync:
                world.tick()
            else:
                world.wait_for_tick()

            # Render received data
            display_manager.render()

            clock.tick_busy_loop(60)
            if controller.parse_events(client, world, clock, args.sync):
                return
            clock.tick()

            for event in pygame.event.get():
                if event.type == pygame.QUIT:
                    call_exit = True
                elif event.type == pygame.KEYDOWN:
                    if event.key == K_ESCAPE or event.key == K_q:
                        call_exit = True
                        break

            if call_exit:
                break

    finally:
        if display_manager:
            display_manager.destroy()

        client.apply_batch([carla.command.DestroyActor(x)
                            for x in vehicle_list])

        world.apply_settings(original_settings)


def main():
    argparser = argparse.ArgumentParser(
        description='CARLA Sensor tutorial')
    argparser.add_argument(
        '--host',
        metavar='H',
        default='127.0.0.1',
        help='IP of the host server (default: 127.0.0.1)')
    argparser.add_argument(
        '-p', '--port',
        metavar='P',
        default=2000,
        type=int,
        help='TCP port to listen to (default: 2000)')
    argparser.add_argument(
        '--sync',
        action='store_true',
        help='Synchronous mode execution')
    argparser.add_argument(
        '--async',
        dest='sync',
        action='store_false',
        help='Asynchronous mode execution')
    argparser.set_defaults(sync=True)
    argparser.add_argument(
        '--res',
        metavar='WIDTHxHEIGHT',
        default='1024x512',
        help='window resolution (default: 1344x376)')

    args = argparser.parse_args()

    args.width, args.height = [int(x) for x in args.res.split('x')]

    try:
        client = carla.Client(args.host, args.port)
        client.set_timeout(5.0)

        run_simulation(args, client)

    except KeyboardInterrupt:
        print('\nCancelled by user. Bye!')


if __name__ == '__main__':
    main()
