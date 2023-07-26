# Core CLR and System.Xml #

If you need to use some package which is dependent on System.Xml with Core CLR / DNX Core you simply add the Nuget package ```System.Xml.XmlDocument``` rather than referencing the GAC System.Xml. 

This allows you to keep your code in Core CLR friendly mode.

![System.Xml.XmlDocument on NuGet](https://eliot-jones.com/images/system-xml/system-xml.png)