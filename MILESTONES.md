# Arduino Configuration Desktop App - Milestones

## Overview
This document breaks down the PRD into progressive milestones, starting with the simplest features and building toward full functionality. No PRD requirements are compromised.

---

## Milestone 1: Project Foundation & UI Shell
**Goal:** Establish the basic application structure with a navigable UI.

### Tasks:
- [ ] Create .NET 8 WinUI 3 project (modern Windows desktop framework)
- [ ] Set up main window with navigation panel
- [ ] Create placeholder panels:
  - Dashboard
  - Inputs
  - Display
  - Output Mapping
  - Testing Panel
- [ ] Implement Light/Dark theme switching
- [ ] Basic application settings persistence (JSON)
- [ ] Set up project folder structure:
  ```
  /src
    /Models
    /Views
    /ViewModels
    /Services
    /Helpers
  /resources
  /tests
  ```

### Deliverable:
A running Windows app with navigable empty panels and theme switching.

### Testing Steps:
- [ ] **T1.1** Launch application - verify it opens without errors
- [ ] **T1.2** Click each navigation item (Dashboard, Inputs, Display, Output Mapping, Testing Panel) - verify panel switches correctly
- [ ] **T1.3** Toggle Light theme - verify all panels update to light colors
- [ ] **T1.4** Toggle Dark theme - verify all panels update to dark colors
- [ ] **T1.5** Close and reopen app - verify theme preference persists
- [ ] **T1.6** Resize window to minimum size - verify layout doesn't break
- [ ] **T1.7** Maximize window - verify layout scales appropriately
- [ ] **T1.8** Check Windows Task Manager - verify no memory leaks after navigation

---

## Milestone 2: Serial Communication & Board Detection
**Goal:** Detect and connect to Arduino Pro Micro and Mega 2560 boards.

### Tasks:
- [ ] Implement serial port enumeration service
- [ ] Auto-detect Arduino boards by VID/PID:
  - Pro Micro: VID 0x2341 or 0x1B4F
  - Mega 2560: VID 0x2341, PID 0x0042/0x0010
- [ ] Establish serial connection (115200 baud default)
- [ ] Implement connection status monitoring
- [ ] Display connected board info on Dashboard:
  - Board type
  - COM port
  - Connection status
- [ ] Handle connection/disconnection events
- [ ] Error handling for:
  - Port not found
  - Port in use
  - Communication timeout

### Deliverable:
Dashboard shows detected Arduino boards with real-time connection status.

### Testing Steps:
- [ ] **T2.1** Launch app with NO Arduino connected - verify "No boards detected" message
- [ ] **T2.2** Connect Arduino Pro Micro - verify it appears in Dashboard within 3 seconds
- [ ] **T2.3** Connect Arduino Mega 2560 - verify it appears with correct board type
- [ ] **T2.4** Verify correct COM port displayed matches Device Manager
- [ ] **T2.5** Disconnect Arduino while app running - verify status updates to "Disconnected"
- [ ] **T2.6** Reconnect Arduino - verify status updates to "Connected"
- [ ] **T2.7** Connect Arduino with another app using the port (e.g., Arduino IDE Serial Monitor) - verify graceful "Port in use" error
- [ ] **T2.8** Connect both Pro Micro AND Mega 2560 simultaneously - verify both detected
- [ ] **T2.9** Open Arduino IDE Serial Monitor on connected board - verify app shows "Port busy" status
- [ ] **T2.10** Close Serial Monitor - verify app can reconnect

---

## Milestone 3: Configuration Persistence
**Goal:** Save and load all configurations in JSON format.

### Tasks:
- [ ] Define configuration data models:
  - BoardConfiguration
  - InputConfiguration
  - DisplayConfiguration
  - OutputMapping
- [ ] Implement JSON serialization/deserialization
- [ ] Create configuration file management:
  - Save configuration
  - Load configuration
  - New configuration
  - Recent configurations list
- [ ] Auto-save on changes (optional toggle)
- [ ] Configuration validation on load

### Deliverable:
Users can save/load complete configurations as JSON files.

### Testing Steps:
- [ ] **T3.1** Create new configuration - verify empty/default state
- [ ] **T3.2** Save configuration to file - verify .json file created with readable content
- [ ] **T3.3** Load saved configuration - verify all data restored correctly
- [ ] **T3.4** Modify configuration and save to different filename - verify both files exist independently
- [ ] **T3.5** Open JSON file in text editor - verify structure is valid JSON
- [ ] **T3.6** Edit JSON file externally to add invalid data - verify app shows validation error on load
- [ ] **T3.7** Test "Recent configurations" list - verify last 5 files appear
- [ ] **T3.8** Click recent configuration entry - verify it loads correctly
- [ ] **T3.9** Enable auto-save, make change - verify file updated without manual save
- [ ] **T3.10** Disable auto-save, make change, close app - verify prompt to save appears
- [ ] **T3.11** Load configuration file from different folder - verify works correctly
- [ ] **T3.12** Try to load non-JSON file - verify graceful error message

---

## Milestone 4: Basic Input Configuration UI
**Goal:** Add and configure inputs with pin assignments.

### Tasks:
- [ ] Create Input Configuration panel UI
- [ ] Support adding input types:
  - KD2-22 Latching Button
  - Momentary Button
  - Toggle Switch
  - EC11 Rotary Encoder
- [ ] Pin assignment interface:
  - Dropdown for available pins
  - Visual pin diagram reference
- [ ] Pin validation:
  - Prevent duplicate pin assignments
  - Show available pins based on board type
  - Inline conflict alerts
- [ ] Input naming/labeling
- [ ] Delete/edit existing inputs
- [ ] "Test" button per input (placeholder for now)

### Pin Mappings:
**Pro Micro:**
- Digital: 2-10, 14-16, A0-A3
- SPI: 14(MISO), 15(SCK), 16(MOSI)

**Mega 2560:**
- Digital: 2-53
- SPI: 50(MISO), 51(MOSI), 52(SCK), 53(SS)

### Deliverable:
Users can add/configure/remove inputs with validated pin assignments.

### Testing Steps:
- [ ] **T4.1** Add KD2-22 Latching Button - verify appears in input list
- [ ] **T4.2** Add Momentary Button - verify appears in input list
- [ ] **T4.3** Add Toggle Switch - verify appears in input list
- [ ] **T4.4** Add EC11 Rotary Encoder - verify appears with 2 pin fields (CLK, DT) + optional SW
- [ ] **T4.5** Assign pin 2 to first button - verify pin 2 removed from available pins dropdown
- [ ] **T4.6** Try to assign pin 2 to second button - verify conflict error displayed
- [ ] **T4.7** Change board type from Pro Micro to Mega 2560 - verify pin list updates (2-53 available)
- [ ] **T4.8** Change board type from Mega 2560 to Pro Micro - verify warning if pins > 16 are assigned
- [ ] **T4.9** Edit input name from "Button 1" to "Start Button" - verify name updates in list
- [ ] **T4.10** Delete an input - verify removed from list and pin becomes available again
- [ ] **T4.11** Add 10 inputs - verify scrolling works in input list
- [ ] **T4.12** Save configuration with inputs - reload - verify all inputs restored with correct pins
- [ ] **T4.13** Click "Test" button - verify placeholder response (no crash)
- [ ] **T4.14** Verify SPI pins (14, 15, 16 on Pro Micro) show warning when assigned to inputs

---

## Milestone 5: Keyboard Output Mapping
**Goal:** Map inputs to keyboard outputs with full key support.

### Tasks:
- [ ] Create Output Mapping panel UI
- [ ] Input-to-output mapping interface:
  - List of configured inputs
  - Keyboard output assignment per input
- [ ] Support mapping types:
  - Single key press
  - Key combinations (Ctrl+, Alt+, Shift+, Win+)
  - Special keys (F1-F24, media keys, navigation)
  - Key sequences
- [ ] Drag-and-drop mapping interface
- [ ] Key capture mode (press key to assign)
- [ ] Real-time keyboard output preview panel
- [ ] Mapping validation:
  - Warn on duplicate mappings
  - Show conflicts

### Key Categories:
- Letters (A-Z)
- Numbers (0-9)
- Function keys (F1-F24)
- Modifiers (Ctrl, Alt, Shift, Win)
- Navigation (Arrows, Home, End, PgUp, PgDn)
- Media (Play, Pause, Vol+, Vol-, Mute)
- Special (Enter, Tab, Esc, Space, Backspace)

### Deliverable:
Full keyboard mapping UI with drag-and-drop and preview.

### Testing Steps:
- [ ] **T5.1** Navigate to Output Mapping - verify all configured inputs from M4 are listed
- [ ] **T5.2** Assign single key 'A' to button - verify mapping displays correctly
- [ ] **T5.3** Assign Ctrl+C combo to button - verify displays as "Ctrl+C"
- [ ] **T5.4** Assign Ctrl+Shift+Alt+F1 combo - verify all modifiers display
- [ ] **T5.5** Use key capture mode - press 'X' key - verify 'X' assigned
- [ ] **T5.6** Use key capture mode - press Ctrl+V - verify combo captured correctly
- [ ] **T5.7** Assign F13-F24 keys - verify they appear (extended function keys)
- [ ] **T5.8** Assign media key (Play/Pause) - verify displays with media icon
- [ ] **T5.9** Assign navigation key (Page Down) - verify displays correctly
- [ ] **T5.10** Drag input to keyboard key visual - verify mapping created
- [ ] **T5.11** Assign same key to two different inputs - verify duplicate warning appears
- [ ] **T5.12** Assign encoder rotation to Arrow Up/Down - verify both directions configurable
- [ ] **T5.13** Preview panel: click mapped button visual - verify key preview highlights
- [ ] **T5.14** Save and reload - verify all mappings persist correctly
- [ ] **T5.15** Delete an input in Input panel - verify mapping auto-removes in Output panel
- [ ] **T5.16** Test key sequence (e.g., "abc") - verify sequence configuration works

---

## Milestone 6: MAX7219 Display Configuration
**Goal:** Configure MAX7219 8-bit 7-segment displays via SPI.

### Tasks:
- [ ] Create Display Configuration panel UI
- [ ] Add MAX7219 display module:
  - Assign CS (Chip Select) pin
  - Set brightness (0-15)
  - Set number of digits (1-8)
- [ ] Encoder-to-display mapping:
  - Select which encoder controls which display
  - Set value increments: 1, 10, 100, 1000
  - Set min/max values
  - Set initial value
- [ ] Conflict detection:
  - Warn when multiple encoders control same display
  - Define priority or behavior rules
- [ ] Display value formatting:
  - Leading zeros
  - Decimal point position
- [ ] Real-time display preview in UI (simulated 7-segment)
- [ ] Support multiple MAX7219 modules (cascaded)

### SPI Configuration:
- Data pin (MOSI): Fixed per board
- Clock pin (SCK): Fixed per board
- CS pin: User-selectable

### Deliverable:
Full MAX7219 display configuration with encoder mapping and conflict handling.

### Testing Steps:
- [ ] **T6.1** Add MAX7219 display - verify appears in display list
- [ ] **T6.2** Assign CS pin 10 - verify pin removed from available inputs list
- [ ] **T6.3** Set brightness to 0 - verify preview shows dimmest
- [ ] **T6.4** Set brightness to 15 - verify preview shows brightest
- [ ] **T6.5** Set digits to 4 - verify preview shows 4-digit display
- [ ] **T6.6** Set digits to 8 - verify preview shows 8-digit display
- [ ] **T6.7** Map encoder to display - verify encoder appears in mapping list
- [ ] **T6.8** Set increment to 1 - rotate encoder visual - verify display changes by 1
- [ ] **T6.9** Set increment to 100 - verify display changes by 100
- [ ] **T6.10** Set min value 0, max 9999 - try to go below 0 - verify stops at 0
- [ ] **T6.11** Set initial value to 500 - verify preview shows 500
- [ ] **T6.12** Map TWO encoders to same display - verify conflict warning appears
- [ ] **T6.13** Configure conflict resolution (e.g., last-write-wins) - verify option saves
- [ ] **T6.14** Enable leading zeros - verify "5" displays as "0005" in preview
- [ ] **T6.15** Set decimal point at position 2 - verify preview shows decimal
- [ ] **T6.16** Add second MAX7219 display (cascaded) - verify both appear
- [ ] **T6.17** Verify MOSI/SCK pins shown as fixed/reserved for board type
- [ ] **T6.18** Save and reload - verify all display settings persist

---

## Milestone 7: Arduino Code Generation
**Goal:** Generate Arduino sketch based on user configuration.

### Tasks:
- [ ] Create Arduino code generator service
- [ ] Generate code for:
  - Pin initialization
  - Input reading (buttons, encoders, switches)
  - Keyboard output (using Keyboard.h for Pro Micro)
  - MAX7219 display control (using LedControl library)
  - Serial communication for testing mode
- [ ] Include required libraries:
  - LedControl or MD_MAX72XX for MAX7219
  - Keyboard.h for HID output
  - Encoder.h for rotary encoders
- [ ] Code optimization:
  - Debouncing for buttons
  - Efficient encoder reading
  - Non-blocking display updates
- [ ] Code preview window
- [ ] Export sketch as .ino file

### Deliverable:
Generated Arduino code ready for upload.

### Testing Steps:
- [ ] **T7.1** Generate code with 1 momentary button mapped to 'A' - verify code includes pinMode, digitalRead, Keyboard.press('a')
- [ ] **T7.2** Generate code with encoder - verify Encoder library included and encoder reading code present
- [ ] **T7.3** Generate code with MAX7219 - verify LedControl library included and SPI init code present
- [ ] **T7.4** Verify generated code includes debounce logic for buttons
- [ ] **T7.5** Preview window: verify syntax highlighting works
- [ ] **T7.6** Preview window: verify code is scrollable for large configurations
- [ ] **T7.7** Export as .ino file - verify file created in chosen location
- [ ] **T7.8** Open exported .ino in Arduino IDE - verify no syntax errors (compiles)
- [ ] **T7.9** Generate code for Pro Micro - verify Keyboard.h used
- [ ] **T7.10** Generate code for Mega 2560 - verify serial-based keyboard approach or warning
- [ ] **T7.11** Generate code with key combo (Ctrl+C) - verify modifier key handling
- [ ] **T7.12** Generate code with multiple displays - verify cascaded display code
- [ ] **T7.13** Verify serial communication code for test mode included
- [ ] **T7.14** Generate with no inputs configured - verify minimal valid sketch
- [ ] **T7.15** Verify code comments explain each section

---

## Milestone 8: Arduino Upload Integration
**Goal:** Compile and upload sketches directly from the app.

### Tasks:
- [ ] Integrate Arduino CLI or arduino-builder
- [ ] Board selection and configuration
- [ ] Compile sketch with progress feedback
- [ ] Upload to connected board
- [ ] Error handling:
  - Compilation errors with line numbers
  - Upload failures with retry
  - Port busy handling
- [ ] Verbose output option
- [ ] Verify after upload

### Deliverable:
One-click compile and upload from within the app.

### Testing Steps:
- [ ] **T8.1** Click "Upload" with no board connected - verify error message
- [ ] **T8.2** Connect Pro Micro, click "Upload" - verify compile starts with progress bar
- [ ] **T8.3** Verify compilation success message shows sketch size
- [ ] **T8.4** Verify upload progress bar during upload
- [ ] **T8.5** Verify "Upload successful" message on completion
- [ ] **T8.6** Introduce syntax error in code (manual edit) - verify compile error with line number
- [ ] **T8.7** Upload to Mega 2560 - verify correct board/port selected automatically
- [ ] **T8.8** Disconnect USB during upload - verify graceful failure and retry option
- [ ] **T8.9** Enable verbose output - verify detailed compile/upload log shown
- [ ] **T8.10** Verify "Verify after upload" option works (reads back)
- [ ] **T8.11** Upload with Arduino IDE Serial Monitor open - verify "port busy" handling
- [ ] **T8.12** Test upload with missing Arduino CLI - verify helpful error message
- [ ] **T8.13** Upload large sketch (near memory limit) - verify size warning
- [ ] **T8.14** Cancel upload mid-process - verify clean cancellation
- [ ] **T8.15** After successful upload, press button on Arduino - verify keyboard output works

---

## Milestone 9: Input Testing Panel
**Goal:** Real-time testing of all inputs with visual feedback.

### Tasks:
- [ ] Create Testing Panel UI
- [ ] Real-time input state display:
  - Button states (pressed/released/latched)
  - Encoder values and direction
  - Switch positions
- [ ] Display value preview (simulated 7-segment)
- [ ] Serial polling:
  - Poll Arduino at configurable rate
  - Parse input state messages
  - Handle communication errors
- [ ] Visual feedback:
  - Color-coded input states
  - Animated encoder rotation
  - Value change highlighting
- [ ] Test mode on Arduino:
  - Special firmware for input testing
  - Send all input states via serial
- [ ] Per-input "Test" button activation
- [ ] Log panel for input events

### Deliverable:
Full real-time testing with visual feedback for all inputs and displays.

### Testing Steps:
- [ ] **T9.1** Open Testing Panel with no board connected - verify appropriate message
- [ ] **T9.2** Connect board with test firmware uploaded - verify "Ready for testing" status
- [ ] **T9.3** Press physical momentary button - verify UI shows pressed state (color change)
- [ ] **T9.4** Release momentary button - verify UI shows released state
- [ ] **T9.5** Toggle latching button - verify UI shows latched ON state
- [ ] **T9.6** Toggle latching button again - verify UI shows latched OFF state
- [ ] **T9.7** Flip toggle switch - verify UI shows correct position
- [ ] **T9.8** Rotate encoder clockwise - verify value increases in UI
- [ ] **T9.9** Rotate encoder counter-clockwise - verify value decreases in UI
- [ ] **T9.10** Verify encoder rotation animation in UI
- [ ] **T9.11** Verify 7-segment display preview updates with encoder value
- [ ] **T9.12** Change polling rate to 10ms - verify faster response
- [ ] **T9.13** Change polling rate to 500ms - verify slower but stable response
- [ ] **T9.14** Disconnect USB during testing - verify graceful disconnect message
- [ ] **T9.15** Reconnect USB - verify testing resumes automatically
- [ ] **T9.16** Check log panel - verify input events logged with timestamps
- [ ] **T9.17** Clear log - verify log emptied
- [ ] **T9.18** Test with 10+ inputs configured - verify all update without lag
- [ ] **T9.19** Verify value change highlighting (flash on change)
- [ ] **T9.20** Click individual "Test" button on input - verify isolated test works

---

## Milestone 10: Wiring Assistance
**Goal:** Generate wiring diagrams and step-by-step guides.

### Tasks:
- [ ] Create Wiring Assistance panel
- [ ] Generate wiring diagram:
  - Based on configured components
  - Show Arduino board pinout
  - Show component connections
  - Color-coded wires
- [ ] Diagram generation approach:
  - Use SVG rendering or
  - Integrate diagram library (e.g., mxGraph, GoJS)
  - Pre-built component graphics
- [ ] Step-by-step wiring guide:
  - Numbered instructions
  - Pin-to-component mapping table
  - Component identification tips
- [ ] Export options:
  - Save diagram as image (PNG/SVG)
  - Print-friendly guide
  - PDF export
- [ ] Component library graphics:
  - Arduino Pro Micro
  - Arduino Mega 2560
  - MAX7219 module
  - KD2-22 button
  - Momentary button
  - Toggle switch
  - EC11 encoder

### Deliverable:
Dynamic wiring diagrams and printable step-by-step guides.

### Testing Steps:
- [ ] **T10.1** Open Wiring Assistance with no inputs configured - verify empty/instruction state
- [ ] **T10.2** Configure 1 button on pin 2 - verify diagram shows Arduino + button + wire
- [ ] **T10.3** Verify wire color matches legend
- [ ] **T10.4** Add encoder (2 pins) - verify both wires shown distinctly
- [ ] **T10.5** Add MAX7219 display - verify SPI connections (MOSI, SCK, CS) shown
- [ ] **T10.6** Verify Arduino Pro Micro graphic matches actual board pinout
- [ ] **T10.7** Switch to Mega 2560 - verify graphic updates to Mega pinout
- [ ] **T10.8** Zoom in on diagram - verify clarity maintained
- [ ] **T10.9** Zoom out on diagram - verify all components visible
- [ ] **T10.10** Check step-by-step guide - verify numbered instructions present
- [ ] **T10.11** Verify pin-to-component table lists all connections
- [ ] **T10.12** Export as PNG - verify image file created and readable
- [ ] **T10.13** Export as SVG - verify vector file created
- [ ] **T10.14** Export as PDF - verify PDF opens with diagram + guide
- [ ] **T10.15** Print preview - verify layout fits on page
- [ ] **T10.16** Configure 15+ components - verify diagram handles complexity
- [ ] **T10.17** Verify component identification tips (e.g., "KD2-22 has 4 pins...")
- [ ] **T10.18** Hover over wire in diagram - verify tooltip shows connection details

---

## Milestone 11: UI Polish & Animations
**Goal:** Achieve modern, polished UI with smooth interactions.

### Tasks:
- [ ] Refine all panel layouts
- [ ] Implement smooth animations:
  - Panel transitions
  - Button feedback
  - List item animations
- [ ] Enhance Light/Dark themes:
  - Consistent color palette
  - Proper contrast ratios
  - Accent colors
- [ ] Add loading states and progress indicators
- [ ] Responsive layout for different window sizes
- [ ] Keyboard navigation support
- [ ] Accessibility improvements (screen reader support)
- [ ] Icon set for all actions
- [ ] Tooltips and help text

### Deliverable:
Polished, professional-looking UI with smooth user experience.

### Testing Steps:
- [ ] **T11.1** Navigate between panels - verify smooth transition animations
- [ ] **T11.2** Click buttons - verify press feedback animation
- [ ] **T11.3** Add item to list - verify item animates in
- [ ] **T11.4** Delete item from list - verify item animates out
- [ ] **T11.5** Light theme: verify all text readable (contrast ratio >= 4.5:1)
- [ ] **T11.6** Dark theme: verify all text readable (contrast ratio >= 4.5:1)
- [ ] **T11.7** Verify accent color consistent across all panels
- [ ] **T11.8** Trigger loading state (e.g., upload) - verify spinner/progress shown
- [ ] **T11.9** Resize to 800x600 - verify no overlapping elements
- [ ] **T11.10** Resize to 1920x1080 - verify layout uses space well
- [ ] **T11.11** Navigate using only Tab key - verify logical tab order
- [ ] **T11.12** Navigate using arrow keys in lists - verify selection moves
- [ ] **T11.13** Press Enter on focused button - verify activates
- [ ] **T11.14** Enable screen reader (Narrator) - verify all elements announced
- [ ] **T11.15** Verify all buttons have icons
- [ ] **T11.16** Hover over icon-only button - verify tooltip appears
- [ ] **T11.17** Hover over complex feature - verify help text appears
- [ ] **T11.18** Check for consistent spacing/margins across all panels
- [ ] **T11.19** Verify no UI flicker during rapid navigation
- [ ] **T11.20** Test with Windows High Contrast mode - verify usable

---

## Milestone 12: Testing & Quality Assurance
**Goal:** Comprehensive testing and bug fixing.

### Tasks:
- [ ] Unit tests:
  - Configuration serialization
  - Pin validation logic
  - Code generation
  - Key mapping
- [ ] Integration tests:
  - Serial communication
  - Full configuration workflow
  - Upload process
- [ ] UI tests:
  - Navigation flows
  - Input validation
  - Theme switching
- [ ] Hardware testing:
  - Test with actual Pro Micro
  - Test with actual Mega 2560
  - Test MAX7219 displays
  - Test all input types
- [ ] Performance testing:
  - Serial polling performance
  - UI responsiveness
- [ ] Bug fixes and refinements

### Deliverable:
Stable, tested application ready for packaging.

### Testing Steps:
- [ ] **T12.1** Run all unit tests - verify 100% pass rate
- [ ] **T12.2** Run unit tests with code coverage - verify >= 80% coverage
- [ ] **T12.3** Run integration tests - verify all pass
- [ ] **T12.4** Run UI automation tests - verify all pass
- [ ] **T12.5** **Hardware: Pro Micro** - Configure 3 buttons + 1 encoder + 1 display - upload - verify all work
- [ ] **T12.6** **Hardware: Mega 2560** - Configure 5 buttons + 2 encoders + 2 displays - upload - verify all work
- [ ] **T12.7** **Hardware: MAX7219** - Verify display shows correct values with encoder rotation
- [ ] **T12.8** **Hardware: KD2-22** - Verify latching behavior works correctly
- [ ] **T12.9** **Hardware: Momentary** - Verify press/release detected correctly
- [ ] **T12.10** **Hardware: Toggle** - Verify ON/OFF states detected
- [ ] **T12.11** **Hardware: EC11** - Verify rotation direction and steps accurate
- [ ] **T12.12** Performance: Poll 20 inputs at 50ms - verify no UI lag
- [ ] **T12.13** Performance: Generate code for complex config - verify < 1 second
- [ ] **T12.14** Performance: Load large config file - verify < 500ms
- [ ] **T12.15** Memory: Run app for 1 hour with active testing - verify no memory leak
- [ ] **T12.16** Stress: Rapidly click all UI elements - verify no crash
- [ ] **T12.17** Edge case: Configure maximum inputs for Pro Micro - verify handled
- [ ] **T12.18** Edge case: 0 inputs configured - verify app still functions
- [ ] **T12.19** Document all bugs found - verify all fixed or documented as known issues
- [ ] **T12.20** Regression: Re-run M1-M11 test cases - verify no regressions

---

## Milestone 13: Packaging & Distribution
**Goal:** Create Windows installer for distribution.

### Tasks:
- [ ] Application signing (optional)
- [ ] Create installer:
  - MSIX package or
  - WiX installer or
  - Inno Setup
- [ ] Include dependencies:
  - .NET 8 runtime (or self-contained)
  - Arduino CLI (bundled or download prompt)
- [ ] Start menu shortcuts
- [ ] File association for .arduinoconfig files
- [ ] Auto-update mechanism (optional)
- [ ] Release documentation:
  - User manual
  - Quick start guide
  - Troubleshooting guide

### Deliverable:
Distributable Windows installer with documentation.

### Testing Steps:
- [ ] **T13.1** Build installer package - verify no build errors
- [ ] **T13.2** Install on clean Windows 10 machine - verify successful install
- [ ] **T13.3** Install on clean Windows 11 machine - verify successful install
- [ ] **T13.4** Verify Start Menu shortcut created and works
- [ ] **T13.5** Verify Desktop shortcut option works
- [ ] **T13.6** Double-click .arduinoconfig file - verify app opens with config loaded
- [ ] **T13.7** Install without admin rights - verify user-level install works (or appropriate prompt)
- [ ] **T13.8** Verify .NET runtime installed or bundled correctly
- [ ] **T13.9** Verify Arduino CLI available after install
- [ ] **T13.10** Uninstall application - verify clean removal (no leftover files)
- [ ] **T13.11** Reinstall after uninstall - verify works correctly
- [ ] **T13.12** Install over existing installation (upgrade) - verify settings preserved
- [ ] **T13.13** Check installer size - verify reasonable (< 100MB for bundled, < 20MB without runtime)
- [ ] **T13.14** Test auto-update (if implemented) - verify update downloads and installs
- [ ] **T13.15** Verify user manual opens from Help menu
- [ ] **T13.16** Verify quick start guide included and accurate
- [ ] **T13.17** Follow troubleshooting guide for common issues - verify solutions work
- [ ] **T13.18** Virus scan installer - verify no false positives (or sign to prevent)
- [ ] **T13.19** Test on low-spec machine (4GB RAM, HDD) - verify acceptable performance
- [ ] **T13.20** Final walkthrough: Fresh install → Configure → Upload → Test - verify complete workflow

---

## Summary: Milestone Dependencies

```
M1 (UI Shell)
  └─> M2 (Serial/Board Detection)
       └─> M3 (Config Persistence)
            ├─> M4 (Input Config)
            │    └─> M5 (Keyboard Mapping)
            │         └─> M6 (Display Config)
            │              └─> M7 (Code Generation)
            │                   └─> M8 (Upload)
            │                        └─> M9 (Testing Panel)
            └─> M10 (Wiring Assistance)
  └─> M11 (UI Polish) [parallel work possible]
       └─> M12 (Testing)
            └─> M13 (Packaging)
```

---

## Technology Stack Summary

| Component | Technology |
|-----------|------------|
| Framework | .NET 8, WinUI 3 |
| Language | C# |
| UI Pattern | MVVM |
| Serial | System.IO.Ports |
| JSON | System.Text.Json |
| Diagrams | SVG generation / SkiaSharp |
| Arduino | Arduino CLI integration |
| Installer | MSIX or WiX |
| Testing | xUnit, Moq |

---

## Hardware Reference

### Arduino Pro Micro (ATmega32U4)
- USB HID capable (native keyboard emulation)
- 18 digital I/O pins
- SPI: 14(MISO), 15(SCK), 16(MOSI)
- 5V logic

### Arduino Mega 2560 (ATmega2560)
- Requires external USB HID (or serial-to-keyboard)
- 54 digital I/O pins
- SPI: 50(MISO), 51(MOSI), 52(SCK), 53(SS)
- 5V logic

### MAX7219 8-bit 7-Segment Display
- SPI interface (3-wire + power)
- DIN → MOSI
- CLK → SCK
- CS → User-defined pin
- VCC: 5V, GND: Ground

---

## Test Summary

| Milestone | Test Count |
|-----------|------------|
| M1: UI Shell | 8 tests |
| M2: Serial/Board | 10 tests |
| M3: Config Persistence | 12 tests |
| M4: Input Config | 14 tests |
| M5: Keyboard Mapping | 16 tests |
| M6: Display Config | 18 tests |
| M7: Code Generation | 15 tests |
| M8: Upload | 15 tests |
| M9: Testing Panel | 20 tests |
| M10: Wiring | 18 tests |
| M11: UI Polish | 20 tests |
| M12: QA | 20 tests |
| M13: Packaging | 20 tests |
| **Total** | **206 tests** |
