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
Once run, this code generates a JSON file (```api-calls.json``` as specified in the cassette constructor):
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
The ```Player``` object is initialized with the (local) base address, where it listens for client requests, and an address of a remote service, to which the requests are redirected when recording. Note that **the player does not intercept HTTP requests**, so the client should send its requests to the player's base address in order for them to be recorded or replayed.
--- tbc ---

[nunit]: http://www.nunit.org/