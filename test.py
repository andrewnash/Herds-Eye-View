from PIL import Image
import numpy as np
import os
import json
import torch

path = 'C:/data/hev/v5_1kUnity_val/'
idx = 1
img = Image.open(os.path.join(path, 'hev', f'{idx}.png'))
img = np.array(img)

colors = np.unique(img.reshape(-1, img.shape[2]), axis=0)


a = np.array([[1, 2, 3], [4, 5, 6], [7, 8, 9]])
a.tolist


batch = dict()

with np.load(os.path.join('C:/data/hev/v4_1kBiasYaw_train/', 'data', f'{idx}.npz'), allow_pickle=True) as data:
    #print(data['aux'][()].items())
    for key, val in data['aux'][()].items():
        batch[key] = torch.from_numpy(val.astype('float32'))

print(batch['intrinsics'].numpy())

new_batch = dict()
with open('C:/data/hev/v5_1kUnity_val/data/1.json') as data:
    for key, val in json.load(data).items():
        new_batch[key] = torch.from_numpy(np.asarray(json.loads(val), dtype='float32'))

print(new_batch['intrinsics'].numpy())

