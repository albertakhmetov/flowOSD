# flowOSD 2

[Download the latest version here](https://github.com/albertakhmetov/flowOSD/releases/download/v2.1.1/flowOSD-2.1.1.exe)

*This version supports only Windows 11 22H2. Please, for Windows 10 and the previous release of Windows 11 use flowOSD 1.x*

<img width="256" alt="Preview3-volume" src="https://user-images.githubusercontent.com/5954460/222946809-2a7b5abe-9834-4a63-9588-a8e04d135a05.png">

<img width="310" alt="preview3-ui" src="https://user-images.githubusercontent.com/5954460/222946644-e967cba8-99cc-40b8-b23b-8cf9e8ca950b.png">

## Disclaimer

There are no warranties. Use this app at your own risk.

## About

flowOSD - is an open source application to control hardware and show OSD messages on [ASUS ROG Flow x13](https://rog.asus.com/laptops/rog-flow/2021-rog-flow-x13-series/) notebooks. For work it requires only ASUS Optimization Service (which is installed with ASUS System Control Interface drivers). I wrote this app to avoid installing MyASUS and Armoury Crate utilites (this is my personal preference).

This app is tested on **GV301QH** model (120Hz WUXGA display). The proper functionality with other modifications are not guaranteed. 

flowOSD shows the following OSDs:

* Keyboard backlight level changing
* TouchPad is disabled/enabled
* Display refresh rate is changed
* CPU Boost is disabled/enabled
* Performance Mode Override (Turbo/Silent)
* System Power Mode
* Discrete GPU is disabled/enabled
* Power source is changed (AC/DC)
* Microphone state (muted/on air)

Also shows the current charge (discharge) rate of the battery. All of these are optional and can be configurable (except keyboard backlight - its indicator always is shown when backlight changes).

Dark and Light system themes are supported.

Dark theme:

https://user-images.githubusercontent.com/5954460/222946632-3448bc60-cbfe-4c94-b993-4432b83a25ec.mp4

<br/>

Light theme:

https://user-images.githubusercontent.com/5954460/222946634-7955ae3b-84de-47c9-bbe9-4a62644bac7f.mp4

<br/>

flowOSD allows you to control the following parameters:

* Processor Boost Mode
* Performance Mode Override (Turbo/Silent)
* System Power Mode (Best Power Efficiency, Balanced, Best Performance)
* Display Refresh Rate (these parameters are set depending on the power source - AC / DC)
* Discrete GPU (you can turn it off to save battery life)

In addition, flowOSD provides customizable shortcuts for the following keys:

* Fn+F4 (AURA)
* Fn+F5 (FAN)
* ROG key (works only if ASUS Armoury Crate Interface is disabled in BIOS)
* Fn+C
* Fn+V

Also flowOSD disables the touchpad when the laptop becomes a tablet (is configurable).

**Note:**

*If ASUS Armoury Crate is installed, please remove hotkeys to avoid conflict with the Armoury Crate.*
