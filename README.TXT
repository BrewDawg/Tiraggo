The Tiraggo C# classes are generated for you by My2ndGeneration.

You only need to run two templates and the process is very easy.

Here are the steps:

1) Register for your free account on My2ndGeneration.com
   https://www.my2ndgeneration.com/
   
You can generate your classes on the My2ndGeneration Cloud or using the My2ndGeneration Desktop, it's your choice.   

==========================================
 Cloud Code Generation
==========================================
Video Presentation
http://www.tiraggo.com/movies/Tiraggo_Cloud.html

==========================================
 Desktop Code Generation
==========================================
Video Presentation
http://www.tiraggo.com/movies/Tiraggo_Desktop.html
   
====================================================
Set Your Connection String
=====================================================
Set the connection string in your app/web config file.
The proper entries were added when you installed the Tiraggo.
You just need to set the connection string.

=====================================================
Assign the Loader
=====================================================
Add this to the Global.asax Applicaton_Start for a web application
or in your Program.cs for a Windows.Forms application.

Tiraggo.Interfaces.tgProviderFactory.Factory = new Tiraggo.Loader.tgDataProviderFactory(); 