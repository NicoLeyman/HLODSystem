# About this repository
This is a fork of the original HLOD repo from 2019. This package is in no way intended to be production ready, let alone be considered an official Unity package. 
It is intended as a potential starting point/reference for users who wish to implement a custom HLOD system in their projects.
Treat it with the same skepticism as you would any sample snippet found on a barely maintained docs page.

While I've tried to address version compatiblity, support for Scriptable Render Pipelines and various UX issues, many other issues (new and old) remain.

//TODO: Version bump and release notes

# HLOD system
The idea of a Hierarchical Level Of Detail system is to improve performance by simplifying the scene hierarchy as the distance from the camera increases.
By combining all of the static objects in a scene, and dividing the resulting mesh using a quadtree or octree, we can replace many objects without (very) noticeable quality loss.
* Improves rendering performance by reducing the number of draw calls.

This HLOD system includes an interface for a swappable batcher. The batcher is responsible for combining objects to build the HLOD tree structure meshes. The behavior of the used batcher can further affect performance.
* The *Material Preserving Batcher* will produce HLOD meshes with many submeshes as it preserves all of the original object's materials. The performance improvements come from its ability to collapse draw calls of objects using the same materials.
* The *Simple Batcher* will crudely atlas materials together, so that each resulting HLOD tree node mesh uses only 1 draw call. Additionally the single material reduces the texture memory/cache pressure for many scenarios, as some of the original individual textures may not be needed up close and can be replaced with the atlases at a distance.

Additonally the HLOD system contains an interface for a swappable Level-Of-Detail generator.
* Improves rendering performance by reducing the geometric complexity of the HLOD meshes.
* The *Unity Mesh Simplifier* is a crude geometry decimator. It reduces geometry until it reaches a certain fraction of the original geometry and lies within a certain min-max range.

A note on HLOD quality:
(H)LOD visual quality should always be evaluated at the distance they are intended to render at, using the project's intended rendering setup.
Artifacts in the HLOD generation such as texture coordinate stretching or missing polygons can be non-issues in the end due to their actual size on screen and limited contribution to the final pixel color due to effects like fog, lighting and post-processing.
Of course, more precise generation will allow using HLODs of the same cost (geometric and texture complexity) at distances closer to the camera, offering better performance gains. 

[Specification Document][specDoc]

| Render image  | Show draw calls | Show draw calls of HLOD |
| --- | --- | --- |
| ![](Documentation~/Images/overview_1.jpg) | ![](Documentation~/Images/overview_2.jpg)  | ![](Documentation~/Images/overview_3.jpg)|

Here is what HLODSystem can look like in a more complex scene.
![](Documentation~/Images/compare.gif)

||DrawCalls|Tris|
|---|---|---|
|Normal|5642|8.0M|
|HLOD|952|3.9M|
|Rate|16.87%|48.75%|

## How to use

TODO: Replace with updated document

Please refer to this document:
[User Guide][userGuide]

TODO: Replace with modern demo project

Also, you can check out [this project][demoProject] that integrates HLOD into the 3D game kit.

## Prerequisites
### Unity
```
Unity Version: 6000.0.35f1
```

### Git 

You need a Git Client which can work with GitHub.
If you don't have Git installed on your machine, download and install it from [Git Home][gitHome].

### Connecting to GitHub with SSH
To clone the project, your Git must be configured to work with SSH Authentication, as HLODSystem uses SSH Authentication to work with Git Submodules. Check [this][gitSSHSetup] link to set up your git to use SSH to connect to GitHub. 

## Getting the project
### Cloning
The project uses a number of other projects as dependencies, and they are included into it as Git Submodules.
To have a fully working project, you should get those submodules included into the project after you clone the project.

First, run the following command to clone the project:
```sh
$ git clone git@github.com:NicoLeyman/HLODSystem.git
```
After cloning is finished, navigate to the root folder of the project, and run the following command to initialize and clone all submodules:
```sh
$ git submodule update --init --recursive
```

### License
Copyright (c) 2025 Unity Technologies ApS
Licensed under the Unity Companion License for Unity-dependent projects see [Unity Companion License][license].
Unless expressly provided otherwise, the Software under this license is made available strictly on an **“AS IS”** BASIS WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED. Please review the license for details on these and other terms and conditions.

[license]: <https://unity3d.com/legal/licenses/Unity_Companion_License>
[gitHome]:<https://git-scm.com/downloads>
[gitSSHSetup]: <https://help.github.com/articles/connecting-to-github-with-ssh/>
[sampleBranch]: <https://github.com/Unity-Technologies/HLODSystem/tree/samples>
[badgesLink]: <https://badges.cds.internal.unity3d.com/badge-gallery/com.unity.hlod?branch=PackageTests&proxied=true>
[demoProject]: <https://github.com/Unity-Technologies/HLODSystemDemo>
[specDoc]: <https://docs.google.com/document/d/1OPYDNpwGFpkBorZ3GCpL9Z4ck-6qRRD1tzelUQ0UvFc>
[userGuide]: <https://docs.google.com/document/d/18HgBIr8oJweKaXtsIHZlh0s5HuXvQVmVfVcMPHAYS1A>
