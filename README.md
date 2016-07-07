[![Build Status](https://travis-ci.org/igudkova/http-mock-player.svg?branch=master)](https://travis-ci.org/igudkova/http-mock-player)

# HTTP Mock Player

The library implements recorder and player of HTTP requests. When recording, a request to a remote service and its live response are serialized to JSON and saved to a file (cassette), so that next time the same request is made through the player it can be served with the previously recorded response. This scenario is common for integration tests running HTTP requests in isolation.

## Installation
```sh
PM> Install-Package HttpMockPlayer
``` 
## Supported .NET versions
The least supported .NET version is 4.5.

## Usage
The following example uses [NUnit testing framework][nunit] together with the HttpMockPlayer library to test a remote API:
```cs
...
MyAPIclient client;
Player player;
Cassette cassette;

[OneTimeSetUp]
public void OneTimeSetUp()
{
    var baseAddress = new Uri("http://localhost:5555/");
    var remoteAddress = new Uri("https://api.myserver.com");

    client = new MyAPIclient(baseAddress);
    
    player = new Player(baseAddress, remoteAddress);
    player.Start();
    
    cassette = new Cassette("/path/to/mock/api-calls.json");
    player.Load(cassette);
}

[OneTimeTearDown]
public void OneTimeTearDown()
{
    player.Close();
    client.Close();
}

[SetUp]
public void SetUp()
{
    var record = TestContext.CurrentContext.Test.Name;
    
    if (cassette.Contains(record))
    {
        player.Play(record);
    }
    else
    {
        player.Record(record);
    }
}

[TearDown]
public void TearDown()
{
    player.Stop();
}

[Test]
public void GetStuff_ReturnsStuff()
{
    var res = client.GetStuff();
    
    Assert.IsNotNull(res);
    ...
}

[Test]
public void CreateStuff_Unauthorized_Throws()
{
    ...
    Assert.Throws<HttpRequestException>(() => client.CreateStuff(param1, param2));
}
```
Once run, this code generates a JSON file (`api-calls.json` as specified in the cassette constructor):
```javascript
[
  {
    "name": "GetStuff_ReturnsStuff",
    "requests": [
      {
        "request": {
          "method": "GET",
          "uri": "https://api.myserver.com/stuff",
          "headers": {
            "Connection": "Keep-Alive",          
            "Host": "api.myserver.com",
            "User-Agent": "MyAPIclient/1.0"
          }
        },
        "response": {
          "statusCode": 200,
          "statusDescription": "OK",
          "content": {
            "id": "a77a6649-1d41-4d80-a763-494640b99a4b",
            "items": []
          },
          "headers": {
            "Status": "200 OK",
            "X-Served-By": "a123456f3b2fa272558fa6dc951018ad",
            "Content-Length": "56",
            "Cache-Control": "public, max-age=60, s-maxage=60",
            "Content-Type": "application/json; charset=utf-8",
            "Date": "Tue, 28 Jun 2016 20:06:06 GMT",
            "Last-Modified": "Tue, 28 Jun 2016 00:40:50 GMT",
            "Server": "myserver.com"
          }
        }
      }
    ]
  },
  {
    "name": "CreateStuff_Unauthorized_Throws",
    "requests": [
    {
      "request": {
        "method": "POST",
        "uri": "https://api.myserver.com/stuff",
        "content": {
          "id": "186b0d73-4ab6-4726-a963-85996944b6b4",
          "items": []
        },
          "headers": {
            "Content-Length": "56",
            "Content-Type": "application/json; charset=utf-8",
            "Expect": "100-continue",
            "Host": "api.myserver.com",
            "User-Agent": "MyAPIclient/1.0"
          }
        },
        "response": {
          "statusCode": 401,
          "statusDescription": "Unauthorized",
          "content": {
            "message": "Requires authentication",
          },
          "headers": {
            "Status": "401 Unauthorized",
            "Content-Length": "37",
            "Content-Type": "application/json; charset=utf-8",
            "Date": "Tue, 28 Jun 2016 20:06:07 GMT",
            "Server": "myserver.com"
          }
        }
      }
    ]
  }
]
```
The `Player` object is initialized with the (local) base address, where it listens for client requests, and an address of a remote service, to which the requests are redirected when recording. Note that **the player does not intercept HTTP requests**, so the client should send its requests to the player's base address in order for them to be recorded or replayed. Remote address can be changed while the player runs, which makes it possible to test distributed web services.

Check the [HttpMockPlayer.Samples][samples] project for more examples.

## Player lifecycle

After initialization the player should be started, so that it can accept incoming requests. It is also required to load a cassette - JSON file where requests will be read from or written to. If the file doesn't exist, it will be created automatically as soon as recording is stopped. 

Once started and loaded, the player can play or record a series of requests (a record). If it is already in operation, it won't switch to another record until stopped.
```
var record = "CreateNewReport";
    
if (cassette.Contains(record))
{
    player.Play(record);
}
else
{
    player.Record(record);
}
...
// authentication request 
// request to fetch additional data
// request to post a new report

player.Stop();
```
When all done the player should be shut down:
```
player.Close();
```
## Serialization to JSON
A HTTP request is recorded as JSON object which stores its HTTP method, resource URI, body data and headers:
```javascript
"request": {
  "method": "GET",
  "uri": "https://api.github.com/repos/igudkova/http-mock-player",
  "headers": {
    "Connection": "Keep-Alive",
    "Cookie": "cookie1=value1; cookie2=value2",
    "Host": "api.github.com",
    "User-Agent": "SampleGithubClient/1.0"
  }
}
```
For a HTTP response its status code, status description, body data and headers are saved:
```javascript
"response": {
  "statusCode": 200,
  "statusDescription": "Ok",
  "content": "<!DOCTYPE html><html></html>",
  "headers": {
    "Connection": "keep-alive",
    "Content-Length": "28",
    "Cache-Control": "no-cache,no-store,max-age=0,must-revalidate",
    "Content-Type": "text/html; charset=UTF-8",
    "Date": "Thu, 07 Jul 2016 01:06:49 GMT",
    "Expires": "Thu, 07 Jul 2016 01:06:50 GMT",
    "Last-Modified": "Thu, 07 Jul 2016 01:06:50 GMT",
    "Set-Cookie": "yp=1470445610.ygu.1; Expires=Sun, 05-Jul-2026 01:06:49 GMT; Domain=.ya.ru; Path=/,yandex_gid=105422; Expires=Sat, 06-Aug-2016 01:06:49 GMT; Domain=.ya.ru; Path=/,yandexuid=1575635411467853610; Expires=Sun, 05-Jul-2026 01:06:49 GMT; Domain=.ya.ru; Path=/",
    "Server": "nginx",
    "X-Frame-Options": "DENY"
  }
}
```
## Exceptions
In certain cases `Player` object throws custom exceptions:
* `CassetteException` if a cassette cannot be read or saved;
* `PlayerStateException` if player is not in a valid state to start an operation (for instance, when `Play()` is not followed by `Stop()`, but instead another `Play()` is called)

If an error occurs while processing a client request, the player wraps an exception into the response message. The status code in this case is set to 551 (play exception), 552 (record exception) or 550 (general player exception).
## Dependencies

* [Newtonsoft.Json][newtonsoft]

[nunit]:      http://www.nunit.org/
[samples]:    HttpMockPlayer.Samples
[newtonsoft]: https://www.nuget.org/packages/Newtonsoft.Json/