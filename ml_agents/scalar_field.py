import numpy as np

def scalar_field(width, height, coords):
  # Create a scalar field with zeros
  field = np.zeros((length, width))

  # Iterate over the coordinates
  for x, y in coords:
    # Propagate the intensity values until the bounds of the array are reached
    for i in range(0, width):
      for j in range(0, height):
        # Calculate the distance from the starting point
        distance = abs(i - x) + abs(j - y)

        # Set the intensity value based on the distance
        field[i, j] += distance

  # Return the scalar field
  return field




import matplotlib.pyplot as plt

# Create the scalar field starting from the middle of the array
length = 200
width = 200
coords = [(100, 100), (100, 101)]
field = scalar_field(length, width, coords)

# Display the scalar field using imshow
plt.imshow(field, cmap='jet')
#plt.set_cmap('gray')
plt.clim(0, field.max())
plt.show()
