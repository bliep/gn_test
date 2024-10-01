# TESTing 123

The service listens to port 15001 by default and handles GET and POST requests as follows:

## Installing and running
```
git clone git@github.com:bliep/gn_test.git
cd gn_test
dotnet build
./bin/Debug/net8.0/service 
```

## GET
```
curl localhost:15001
```
will return a list of found Bluetooth devices as e.g.:
```
(base) adev@mac ~ % curl localhost:15001
addr=70:BF:92:D0:DC:63, connected=False, Jabra Elite Active 75t
addr=70:F2:7A:EE:E6:63, connected=False, 
addr=48:E1:5C:64:BC:8F, connected=False, 
addr=37:F2:D2:9E:8D:95, connected=False, 
addr=65:C9:73:F0:38:78, connected=False, 
addr=64:A2:62:F9:BE:15, connected=False, 
addr=4D:98:F9:F0:D2:A0, connected=False, 
addr=57:62:50:FD:41:4C, connected=False, 
addr=65:74:F4:01:3C:08, connected=False, 
addr=6C:64:C4:55:52:A2, connected=False, 
addr=5C:05:03:87:A3:82, connected=False, 
addr=6C:70:CB:8F:2D:89, connected=False, 65" QLED
addr=E2:BB:9E:8E:55:4D, connected=False, ET-2820 Series

```
Note that not every device has a name embedded in their advertisement packet, so the name's not always present.

## POST
```
curl -v -X POST -H 'Content-Type: application/json' -d '{"addr": "70:BF:92:D0:DC:63", "connected": true}' localhost:15001
```
will try to set the connected state of the device mentioned by addr to `true` (and similarly to `false` below).
```
curl -v -X POST -H 'Content-Type: application/json' -d '{"addr": "70:BF:92:D0:DC:63", "connected": false}' localhost:15001
```
The HTTP response gives the result of the connection or disconnection operation and is one of 
 - `HttpStatusCode.NotFound` if the addr is not found in the bluetooth scan table
 - `HttpStatusCode.Unauthorized` if the connection or disconnection is unsucessful.
 - `HttpStatusCode.OK` if the operation succeeded.
 - `HttpStatusCode.ServiceUnavailable` if the server failed bad.

## Notes
 - be gentle, the code is fresh,
 - add unit tests if there is more code,
 - enjoy :)
