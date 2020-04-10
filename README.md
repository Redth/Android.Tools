# Android SDK Tools
.NET Library and global dotnet tool for various android adb, avd, and emulator needs.

![.NET Core](https://github.com/Redth/AndroidSdk.Tools/workflows/.NET%20Core/badge.svg)

## Usage

Download/Install the SDK:

```csharp
var sdk = new AndroidSdkManager("/path/to/desired/android_home");
sdk.Acquire();
```

### SDK Manager

List packages that can be installed:

```csharp
var list = sdk.SdkManager.List();

foreach (var a in list.AvailablePackages)
    Console.WriteLine($"{a.Description}\t{a.Version}\t{a.Path}");

foreach (var a in list.InstalledPackages)
    Console.WriteLine($"{a.Description}\t{a.Version}\t{a.Path}");
```

Install a particular package:

```csharp
// The `SdkPackage.Path` is used to specify the package to install
// it might look something like: build-tools;29.0.3
var installPath = list.AvailablePackages.FirstOrDefault().Path;

sdk.SdkManager.Install(installPath);
```


### ADB (Android Debug Bridge)

Execute ADB Commands:

```csharp

// Stop/start adb
sdk.Adb.KillServer();
sdk.Adb.StartServer();

// Find all ADB attached devices
var devices = sdk.Adb.GetDevices();
foreach (var device in devices)
    Console.WriteLine($"ADB Device: {device.Serial}");

// Find an emulator
var emulator = devices.FirstOrDefault(d => d.IsEmulator);

// Get the name of an emulator/device
var emulatorName = sdk.Adb.GetDeviceName(emulator.Serial);

// Use the emulator's serial in other adb calls
// Useful if there's multiple devices connected
var serial = emulator.Serial;

// Push files
sdk.Adb.Push(new FileInfo("/some/image.png"), new FileInfo("/sdcard/image.png"), serial);

// Pull files
sdk.Adb.Pull(new FileInfo("/some/log.txt"), new FileInfo("/local/log.txt"), serial);

// Install an apk
sdk.Adb.Install(new FileInfo("/some/local/app.apk"), serial);

// Uninstall a package
sdk.Adb.Uninstall("com.some.app", keepDataAndCacheDirs: false, serial);

// Dump logcat lines
List<string> logs = sdk.Adb.Logcat(serial);

// Execute shell commands
var output = sdk.Adb.Shell("ls -l", serial);

// Screen capture
sdk.Adb.ScreenCapture(new FileInfo("/local/place/to/save/screen.png"), serial);
```


## AVD Manager

Create an emulator (AVD) definition:

```csharp
var avdSdkId = "system-images;android-29;google_apis_playstore;x86_64";

// Make sure the emulator image we want to use is installed
sdk.SdkManager.Install(avdPackageId);

// Create an Emulator instance
sdk.AvdManager.Create("AVD_Name", avdSdkId, "pixel", force: true);
```


## Emulator

Execute Emulator Commands:

```csharp
// Get a list of available emulators
var avds = sdk.Emulator.ListAvds();

var avd = avds.FirstOrDefault(a => a.Name == "AVD_Name");

// Start the emulator
var emulatorProcess = sdk.Emulator.Start(avd.Name, new EmulatorStartOptions { NoSnapshot = true });

// Wait for the emulator to be in a bootcomplete state, ready to use
emulatorProcess.WaitForBootComplete();
```


## A Complete Example

A common scenario is to create, start, and deploy an apk to an emulator in CI:

```csharp
// Define the emulator image to use, this is an SDK Manager package id
var AVD_SDK_ID = "system-images;android-29;google_apis_playstore;x86_64";

// API Levels we need to install to build our app
var SDK_PACKAGES = new [] { "platforms;android-21", "platforms;android-26", "platforms;android-29" };

// The name of the emulator AVD instance we will create
var AVD_NAME = "CI_Emulator";

var APP_PROJECT = "MyAndroidApp.csproj";
var APP_PACKAGE_NAME = "com.myapp";
var APP_CONFIG = "Release";
var APP_APK = $"{APP_PROJECT}/bin/{APP_CONFIG}/MonoAndroid90/{APP_PACKAGE_NAME}.apk";

// Make sure all of the tools we need are created and installed
var sdk = new AndroidSdkManager();

// Ensure all the SDK components are installed
sdk.Acquire();

// Install the API levels we need for our app
sdk.SdkManager.Install(SDK_PACKAGES);

// Build our Xamarin Android App
var p = Process.Start($"msbuild /p:Configuration={APP_CONFIG} {APP_PROJECT}");
p.WaitForExit();

// Make sure the emulator image we want to use is installed
sdk.SdkManager.Install(avdPackageId);

// Create an Emulator instance
sdk.AvdManager.Create(AVD_NAME, AVD_SDK_ID, "pixel", force: true);

// Start the emulator
var emulatorProcess = sdk.Emulator.Start(AVD_NAME, new EmulatorStartOptions { NoSnapshot = true });

// Wait for the emulator to be in a bootcomplete state, ready to use
emulatorProcess.WaitForBootComplete();

// Install the APK we built for our app earlier
sdk.Adb.Install(APP_APK  emulatorProcess.Serial);

// Launch UI Tests or the app
// TODO: YOUR CODE HERE
// eg: Launch the app we just installed
sdk.Adb.LaunchApp(APP_PACKAGE_NAME, emulatorProcess.Serial);

// TODO: Run some tests?

// Clean up the emulator
emulatorProcess.Shutdown();
```

