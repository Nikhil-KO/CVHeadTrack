# CVHeadTrack
Custom made, Computer Vision based head tracking software made using OpenCV and SimConnect API for Flight Simulator 2020.
Progress

Currently can connect to webcam/ip camera url and detect facial landmarks. Decent range of head turn. 
<img src="https://lh3.googleusercontent.com/Gw2wuQuHnWtDOu9vwblvoahnzVj2mw9sqa363riS02qS5wuP8MuywHKGsUpBB2j3_3Yiatvdjn0_lLcKEEeAdxV2r7dLc1HvWVcKFC5Nic1dcMkIcieTYW9JdRpmE3wRav-eMr7BJ7_d3TGz4Jx7SeD9CtMw5VVsCgy0Y4bva_Axl9u_AeBGtG_Apc-P6llVcivxaIaZFn8_iAEcu5eA1qyrpwskroqj-CrP7NyiubCRRP1l8RUgckZdlClUlGIAP5UTBkecPZ5R8XkDStU5i_RpGCcQzKuhOHFMLe_BYS4n7uMmWZNpwufapL48rj19A32QlVMfZUyzOA_n86Swsdlt_ryYTe08lavEN8BfN33eChHVLxOctNQj8HdAAtuKq45nEHqF0-iK-te73qoEagWRnmc09Knoxe3J7f4c2_66eLDKL7PYTRqjuUMxiWDccdbafsJf8xNv6hSHAdehDT6YoEoDWPWLbGtPEm_8QYgklDwDYBLlZJ1ONUVs_zbkUHysihyPmARiwPvyE5vbrnKMVmfucdjtirF2gpEvvtvvBDjeW6y_-Qo2wuCNAQ114r2LpcjT4NnVuy8yGlNZ-vBM3l3CbXr3nmSUDfjRJct-93A926CQ2__GillM7yd7T-vEHhpbBHWa_bKf8AsCaonbZt-1BQd1vmhh7Ew_tSUCIl24uhQ6blhMFD1pjw=w436-h319-no?authuser=0" alt="Sample face tracking" height="200px"/>

Next step is to parse the face data to a software like OpenTrack or wait for flight sim team to fix/implement CameraRelative6DOF function. 
Prefer to go direct to game but I guess publishers don't care about dev kits.
<br>
<img src="https://i.imgur.com/7K7Thtq.jpg" alt="Sad Keanu" height="50px"/>

# References:

## Modules used

1. DlibDotNet (face nn)
2. OpenCvSharp (to read from ip camera) - shouldn't need if you afford a real webcam
3. SimConnect (broken), waiting on flight sim devs to fix/implement CameraRelative6DOF function

## Bib
* C# Facial landmarks:
https://medium.com/machinelearningadvantage/detect-facial-landmark-points-with-c-and-dlib-in-only-50-lines-of-code-71ab59f8873f
* Read from opencv wrapper to dlib
https://github.com/takuya-takeuchi/DlibDotNet/blob/master/examples/WebcamFacePose/Program.cs