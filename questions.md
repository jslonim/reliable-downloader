# Questions

## How did you approach solving the problem?
Downloading the whole file in one request was the first part, it involved using HttpCompletionOption.ResponseHeadersRead in the request which allowed me to write the file while the content is being loaded from the request.
However, it's clear that resilience was key, so when it came to download the file in parts, my thinking was first to try to download the whole thing and if any interruptions presented, continue from that point on. But when we create a request, the content needs to load before we can start writing it into a file and if the connection is slow or the file is too big, it could take forever to do so. So what I thought to do is dividing the request in smaller parts (1% each) so it would be faster to load the content of each request and if any connection problems came up, we could just finish writting that 1% we already downloaded and continue from that point forward.

That solution worked really well, is fast and allows the user to continue the download right were the connection was lost.


## How did you verify your solution works correctly?
I used NetLimiter to slow my connection so it would take longer to download the file and then turning off the Wifi of my laptop to simulate a disconection.
Also verification with MD5 to check the file was downloaded correctly, as seen in the code.

## How long did you spend on the exercise?
I downloaded the exercise on thursday to have a look but started working on it today (Friday), it took me between 4 and 5 hours. I tried an approach for interrupting the request made with HttpCompletionOption.ResponseHeadersRead with a timer since it wont throw an exception due to the completion of the request (even though the content keeps streaming). I kept it because it allows me to write on real time, show the user the progress and also goes on by itself if the connection comes back since it does not obey timeout.


## What would you add if you had more time and how?
-I would add more tests, 
-Analyze the code to include every possible defensive code meassure (validations, null checkings, etc)
-Using dependency injection instead of creating an instance of the service.



