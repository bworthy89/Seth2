You are an expert software architect and developer.

Using the following Product Requirements Document (PRD), generate the codebase structure, key modules, and example code snippets for a Windows desktop application that allows users to configure and test Arduino Pro Micro and Mega 2560 boards with customizable inputs and MAX7219-based 7-segment displays. The app should support real-time input testing, keyboard output mapping, and wiring diagram generation.

Product Requirements Document (PRD):



Project Title: Arduino Configuration Desktop App



Platform: Windows Desktop



Goal:

Create a modern, intuitive Windows application for configuring and testing two Arduino boards (Pro Micro and Mega 2560) with customizable inputs, MAX7219-based 7-segment displays, keyboard output mappings, real-time input testing, and wiring guidance.



Objectives:

* Simplify Arduino programming without manual coding.
* Provide an intuitive UI for configuring inputs, outputs, and displays.
* Enable real-time input testing with visual feedback.
* Support MAX7219-based 7-segment displays via SPI.
* Generate wiring diagrams and step-by-step guides dynamically.



Key Features:

* Hardware Support: Arduino Pro Micro and Mega 2560, KD2-22 Latching Button, Momentary Buttons, EC11 Rotary Encoders, Toggle Switches, MAX7219-based 0.36" 8-bit 7-segment LED modules.
* Configuration: Add/configure inputs with pin assignments, map encoders to display values (increments: 1, 10, 100, 1000), define button rules, map inputs to keyboard outputs (single keys, combos, special keys).
* Display Control: SPI-based communication for MAX7219, prevent conflicts when multiple encoders control the same display.
* Input Testing: Test buttons, encoders, and displays in real time, with a visual feedback panel.
* Wiring Assistance: Generate wiring diagrams based on user-selected components, provide step-by-step wiring guide with pin assignments.



UI Design Requirements:

* Modern, clean Windows UI with Light/Dark themes.
* Panels: Dashboard (connected boards/status), Inputs (add/configure/test), Display (MAX7219 mapping/live preview), Output Mapping (keyboard assignments), Testing Panel (real-time feedback).
* Drag-and-drop mapping for inputs to outputs.
* Real-time keyboard output preview.



Technical Requirements:

* Language: C# (.NET 8) or Electron (JavaScript/TypeScript).
* Communication: Serial over USB; SPI for MAX7219.
* Arduino Libraries: LedControl or MD\_MAX72XX.
* Persistence: Save/load configurations in JSON.
* Error Handling: Conflict detection and inline alerts.



Milestones and Detailed Specs:



1. Core Framework: Detect Arduino boards, establish serial communication, initialize SPI for MAX7219.

2\. Input Configuration: Add/configure inputs, assign pins, add “Test” button, prevent duplicate pin assignments.

3\. Display Configuration: Add MAX7219 display, assign CS pin, map encoders, validate conflicts, real-time preview.

4\. Output Mapping: Map inputs to keyboard outputs, support combos/special keys, preview mapping.

5\. Input Testing Panel: Real-time feedback for all inputs/displays, poll Arduino states via serial, dynamic UI updates.

6\. UI Polish: Modern design, Light/Dark themes, smooth animations.

7\. Wiring Assistance: Generate wiring diagrams and step-by-step guides based on configuration.

8\. Testing \& Packaging: Unit/integration tests, Windows installer.



Instructions:

* Propose a high-level codebase structure (folders, main modules, key classes/components).
* Provide example code snippets for:

&nbsp;	Serial communication with Arduino

&nbsp;	SPI communication with MAX7219

&nbsp;	Real-time input polling and UI updates

&nbsp;	Keyboard output mapping

&nbsp;	Dynamic wiring diagram generation (describe approach if code is not feasible)

* Suggest libraries/tools for UI, serial/SPI, and diagram generation.
* Include comments and explanations for each part.
