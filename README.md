# WebAdsDemo

## Introduction
This solution works with [SWGeneratorSolution](https://github.com/amirdoosti6060/SWGeneratorSolution) to demonstrate how can control Beckhoff PLC using ADS from Web in C#.  
In this sample you can read and write different types of variables in Beckhoff PLC through ADS as well as receiving notification when a specific variable is changing in the PLC.   

## Structure of soution
The solution contains a Web API .Net6 project which is written in Visual Studio. The web server is Kestrel so it doesn't need any other web server.   

## Technology stack
- OS: Windows 10 Enterprise - 64 bits
- IDE: Visual Studio Enterprise 2022 (64 bits) - version 17.8.3
- Framework: .Net 6
- Language: C#
- Beckhoff TwinCAT Ads 6.1.203

## How to run
Open the solution in Visual Studio and publish the project. Then move the published folder to the machine running PLC.  
Note that before running the project, make sure that PLC is running on AmsId 127.0.0.1.1.1 and port 851.   
Run the executable file then (WebAdsDemo.exe).   
Then open a browser and browse to http://localhost:5000/index.html   
Also note that you can change the AmsId and port number on appsettings.json if needed.   

