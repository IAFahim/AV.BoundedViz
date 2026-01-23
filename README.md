# AV.BoundedViz

![Header](documentation_header.svg)

[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-000000.svg?style=flat-square&logo=unity)](https://unity.com)
[![License](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](LICENSE.md)

Professional Inspector property drawers for bounded values (Health, Mana, Timers).

## ✨ Features

- **Visual Bars**: Renders fields as progress bars in the Inspector.
- **Scrubbing**: Click and drag on the bar to modify values directly in the Inspector.
- **Auto-Detection**: Automatically works with structs containing `Current`, `Max`, and optional `Min` properties.
- **Customizable**: Configurable heights, colors, and gradients via `GameVariableConfig`.

## 📦 Installation

Install via Unity Package Manager (git URL).

### Dependencies
- **Variable.Timer** (NuGet)
- **Variable.Bounded** (NuGet)

## 🚀 Usage

This package automatically applies to supported types (like `Timer` or `Cooldown`). No additional attributes are required for these standard types.

To verify or configure visualization:
1. Locate `GameVariableConfig` asset (or create one in `AV/BoundedViz`).
2. Adjust colors, rounding, and height.

## ⚠️ Status

- 🧪 **Tests**: Missing.
- 📘 **Samples**: None.
