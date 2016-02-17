# code adpated from https://subluminal.wordpress.com/2008/07/31/running-standard-deviations/#more-15

import math

n=0
mean=0
pwrSumAvg=0
stdDev=0
list_of_values = [3.0, 5.0, 8.0, 10.0, 4.0, 8.0]

for x in list_of_values:
    n += 1
    mean += (x - mean) / n
    pwrSumAvg += ( x * x - pwrSumAvg) / n
    if n - 1 > 0:
        stdDev = math.sqrt( (pwrSumAvg * n - n * mean * mean) / (n - 1) )
    print stdDev, mean