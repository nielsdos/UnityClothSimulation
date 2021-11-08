
# Unity Cloth Simulation with CPU/GPU

This repository contains the code for a CPU / GPU cloth simulation. The GPU simulation uses compute shaders to calculate the forces and to apply collisions. The model of the cloth is based around a traditional spring model using three types of springs:
* Bend ( = flexion) springs
* Shear springs
* Elastic ( = structural) springs

See https://www.researchgate.net/figure/Basic-structure-of-the-mass-spring-cloth-model_fig1_331620331 for more information.

## Features
* CPU Cloth simulation with integration types: Euler, RK4, Verlet
* GPU Cloth simulation (only integration type is RK4)
* Prototype of a mesh to cloth simulation
* Highly configurable cloth
* Spatial hasher for CPU simulation
* Inter-cloth collisions
* Prototype cloth grabbing mechanism to attach cloths to a static object or moving object

## Included assets
There are included assets for demo purposes. These are not mine and belong to the respective owners.
List of assets:
* https://sketchfab.com/3d-models/men-polo-t-shirt-pbr-ac1524439a8d472eb38386c02bf4cdf9 by Feelgood
Licensed under CC Attribution
* https://sketchfab.com/3d-models/blue-jeans-pants-6fe45842b1924f6fbf9e8bb021c034c6 by Sirenko
Licensed under CC Attribution
* Table cloth texture by juliosillet: https://gumroad.com/juliosillet?sort=page_layout

## License
MIT license except the included assets.
