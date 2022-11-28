# Microsoft Performance Tools Linux / Android - Developer Information

# Prerequisites

See [Readme Dev PreReqs](Readme.md#Dev%20prereqs)

# Code Editing

## Entire Project
If working on the entire project, or editing .sln or .csproj files 
[Visual Studio](https://visualstudio.microsoft.com/) is recommended

Open [Microsoft-Perf-Tools-Linux-Android.sln](Microsoft-Perf-Tools-Linux-Android.sln) in VS

## Single Files
Use your favorite editor

## Build & Test

### Cross Platform Cmd-Line
- ```dotnet build```
- ```dotnet test```

### IDE
- VS Build Solution or Build Project

# Debugging & Testing

## Dev inner loop
It's often fastest to debug the Unit Test examples since they wrap the plugins. This method keeps runtime overhead to a minimum. See the various *UnitTest projects

- VS Test Explorer is a great way to visualize / run / debug tests. Test -> Test Explorer

## Plugin visualization and trace testing
- After getting some stabilization in a plugin, it's often fastest to test or investigate multiple traces using a GUI.

- The plugins are not tied to any specific GUI. However the GUI does need to support the [Microsoft Performance Toolkit SDK](https://github.com/microsoft/microsoft-performance-toolkit-sdk)

### WPA GUI
- Debugging using WPA
  - We have not figured out to debug attach using Store versions of WPA. Therefore, as a developer, you will need to use a non-Store version
  - For external devs outside Microsoft, use latest [Windows Insider Preview ADK](https://www.microsoft.com/en-us/software-download/windowsinsiderpreviewADK) and install just WPA
  - For internal devs inside Microsoft, use the latest internal WPA
- Using VS2022 Launch Profiles
  - To Start WPA with your plugin (doesn't auto-open file)
    - Executable
        - "C:\PATH\TO\wpa.exe"
    - Command line arguments - 
      - -addsearchdir "C:\src\Microsoft-Performance-Tools-Linux-Android\ThePlugin\bin\Debug"
  - To Start WPA with your plugin AND auto-open file
    - Executable
        - "C:\PATH\TO\wpa.exe"
    - Command line arguments - 
      - -addsearchdir "C:\src\Microsoft-Performance-Tools-Linux-Android\ThePlugin\bin\Debug" -i "C:\PATH\TO\tracefile.ext"