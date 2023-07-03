import re
import statistics

# open and read the file
with open('times/push_block.txt', 'r') as file:
    data_str = file.read()

# extract numbers
numbers = re.findall(r'^\d{1,4}$', data_str, re.MULTILINE)
# convert to int
numbers = [int(num)/5 for num in numbers]

mean = statistics.mean(numbers)
variance = statistics.variance(numbers)
std_dev = statistics.stdev(numbers)

print("Mean:", mean)
print("Variance:", variance)
print("Standard Deviation:", std_dev)