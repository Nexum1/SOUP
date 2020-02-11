# SOUP

### What is SOUP? ###

Other than a delicious snack, SOUP is a brilliantly simple and ressiliant data transfer protocol that brings together my personal favourite parts of SOAP and REST.

### How do I get set up? ###

* Download Repo
* Build the SOUP project
* Reference Soup.dll in both Server And Client
* Create your server methods
```
public class TestServer
{        
    public string TestMethod()
    {
        return "Hello World!";
    }
}
```
* Host the TestServer class by starting up the server
```
SoupServer<TestServer> server = new SoupServer<TestServer>(true, hostUrl: "http://*:8090/");
server.Run();
```
* Copy the SoupClient.tt file to your client project
* In SoupClient.tt, change the url to the appropriate server URL that you chose. Run the TT file.
* In your client you will now be able to call TestServer_Calls.TestMethod()
  This will handle all the calls, do all the serialization and just give you back the results in the model that you expect.
  
### Features ###

* Quick and Easy To Set Up a Client-Server System
* Automatic Model Creation
* Serialization and Deserialization handled in the background
* Low overhead (REST)
* Optional Authentication
* Optional Synchronous requests instead of Async

### Contact ###

* If you have any ideas or issues, the issue tracker is your friend. Alternatively please email me (Corne Vermeulen) on Nexum1@live.com
