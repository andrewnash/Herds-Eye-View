import re
import statistics

file_name = 'times/pushblock_bevf.txt'

# open and read the file
with open(file_name, 'r') as file:
    data_str = file.read()

# extract numbers
numbers = re.findall(r'^\d{1,4}$', data_str, re.MULTILINE)
# convert to int
numbers = [int(num)/10 for num in numbers]

mean = statistics.mean(numbers)
variance = statistics.variance(numbers)
std_dev = statistics.stdev(numbers)

print("file:", file_name)
print("Mean:", mean)
print("Variance:", variance)
print("Standard Deviation:", std_dev)