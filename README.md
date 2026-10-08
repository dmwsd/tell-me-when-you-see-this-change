# "Tell Me When You See This Change"

![Demo](/docs/img/demo.gif)

## Intro

"Tell Me When You See This Change" is a small Windows utility that lets you have your computer emit a sound when a spot on the screen changes color. The basic steps are
    - User picks a pixel from the screen
    - User "arms" the alert
    - When the selected pixel changes color, the computer emits a tone.

This can be used any time you want to know when something changes on your screen while you are in earshot of your computer, but not able to be in front of your computer screen. For example, if you are working on a machine and want to know if a sensor/switch is working when you actuate it (Controls Engineers encounter this, IYKYK)

#### Utility

![Tell Me When You See This Change](/docs/img/screenshot.png)


#### Pixel Selector

![Pixel selector](/docs/img/pixel_selector.png)


#### Demo (same as above)

![Demo](/docs/img/demo.gif)

## How to access this tool

### Options 1 - Download the .exe from the release

Go to the [Releases](https://github.com/dmwsd/tell-me-when-you-see-this-change/releases) section of the GitHub Repo and download the exe file. I promise its not a virus, but of course you probably shouldn't trust promises from strangers on the internet.

### Options 2 - Build the app yourself

Run the build command (in Powershell, `cd` to the repo folder and then `.\build.cmd`). That's it. The necessary compiler and framework is already in Windows 10/11.

### Options 3 - Vibecode the app yourself

Yeah, I didn't write this by hand. I've included my original prompt AND the intermediate plan document in the `docs` folder. The model I used was Claude Opus 5.5 (medium-effort).

## History

I've had the general idea for this since maybe 2013, but I know how to write IEC61131-3 Structured Text (and Ladder!!), not C#. I'm not a Windows app developer.

But in 2026, it's barely harder than writing a paragraph describing what you want. Writing this README by hand is taking me many times longer than the first version of the app took to generate and test. In fact, the agent made a functional version in one-shot (though I did iterate a bit on styling and minor features).

I hope you find it useful!
