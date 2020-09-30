# About Editor Iteration Profiler
The Editor Iteration Profiler (EIP) is a tool that you can use alongside [Unity's built-in Profiler](https://docs.unity3d.com/Manual/Profiler.html) to monitor Editor iteration (domain reload) times. It helps you to understand why Unity takes a long time to compile your scripts or enter Play Mode. 

An iteration is a process that contains instructions that it repeats until a condition is met. Unity relies on a variety of iteration types. The EIP uses the data from the Profiler to monitor iterations that relate to the scripting side of Unity, specifically:

* Entering and exiting Play Mode
* Assembly reloads
* Script compilations

During these iterations, the Profiler also monitors Asset import time. 

The EIP saves all of this information in a separate window which you can then use to navigate through the data the Profiler produces. What’s more, the data that the EIP collects persists for the lifetime of the Editor, unlike the built-in Profiler which is limited to storing a set number of frames. 

You can then [export the captured data](exporting-data.md) to either HTML, JSON, CSV, or plain text format. In addition to these formats, you can export the data to a HTML Performance Report, which groups the data together to make it easier to see areas you can optimize.

## Preview package
This package is available as a preview, so it is not ready for production use. The features and documentation in this package might change before it is verified for release.

## Package contents
The following table describes the package folder structure:

|**Location**|**Description**|
|---|---|
|`com.unity.editoriterationprofiler`| Contains the code for this package.|
|`com.unit.editoriterationprofiler.tests`|Contains the unit tests for the package.|
|`Documentation~`| Contains the documentation for the package.|
|`UnityEditorIterationProfilerExample`|Contains an example Project.|
|`ThisProjectisWeird`| Contains an example Project.|


## Installation
To install this package, perform the following steps:

* Download the repository from GitHub
* Place it into your Project’s `Packages` folder

## Requirements
This version of Editor Iteration Profiler is compatible with the following versions of the Unity Editor:

* 2019.2 and later

## Known limitations
The Editor Iteration Profiler package has the following known limitations:

* During Deep Profiling the system might run out of memory and freeze the whole Editor for extended periods of time, especially in large Projects.
* If the script compilation taxes more than 600 frames, the EIP might not capture all data.

## Feedback and troubleshooting
If you have any feedback on this package, or want to report a bug, see the [Unity Profiler forum](https://forum.unity.com/forums/profiler-previews.199/). 

If you encounter any problems with the EIP, either select the __Clear__ button in the toolbar of the EIP window, or go to __Window &gt; Analysis &gt; Editor Iteration Profiler &gt; Purge Cache__ to clear the EIP’s cache. This usually resolves most issues.
