from PIL import Image
import numpy as np
import os

path = 'C:/data/2022-9-27_21-50-39/'
idx = 0
img = Image.open(os.path.join(path, 'hev', f'{idx}.png'))
img = np.array(img)

colors = np.unique(img.reshape(-1, img.shape[2]), axis=0)
