# Desktop Organizer

A small Windows desktop experiment built out of curiosity.

I wanted to understand how a real native desktop application works beyond the browser — how an application can interact with Windows itself, access system information and become part of the desktop experience.

What started as a simple UI experiment quickly turned into a small desktop organizer built with **C#, WPF and .NET 10**.

## Preview

![Desktop Organizer Preview](./assets/screenshots/desktop-organizer.png)

## Features

- Custom desktop Island
- Live CPU usage
- Live RAM usage
- Current weather based on location
- Current media information when music is playing
- Automatic RAM fallback when no media is active
- Custom wallpaper integration
- Transparent taskbar overlay
- Custom neon taskbar border
- Click-through overlays that preserve normal Windows interaction
- Single-instance application handling

## The Idea

The goal was not to build another traditional Windows application with buttons, forms and separate windows.

Instead, I wanted the application to feel like part of the desktop itself.

The interface combines system information with a custom visual design while leaving the normal Windows desktop, taskbar and applications usable underneath.

The project also became an opportunity to experiment with areas I normally do not encounter in frontend development, including:

- WPF and XAML
- native Windows APIs
- window positioning and Z-order
- transparent and click-through windows
- system performance counters
- location and weather data
- Windows media sessions
- taskbar integration

## Tech Stack

- C#
- WPF
- XAML
- .NET 10
- Windows APIs / Win32
- Windows Runtime APIs

## Why I Built It

I was curious about native desktop development and wanted to understand how a Windows application behaves compared to the web applications I usually build.

Instead of first learning the entire WPF and Windows API ecosystem, I deliberately used AI to help me build and explore the application.

The goal was not to demonstrate that I already knew WPF or native Windows development. The goal was to learn by building: understanding XAML, window behavior, system APIs, overlays, transparency, positioning, media sessions and how a desktop application interacts with the operating system.

What started as a small experiment with a desktop "Island" gradually evolved into a custom desktop environment with live system information, weather, media information, wallpaper integration and a taskbar overlay.

This is an experimental learning project built with AI assistance.
