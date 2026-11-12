using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Windows.Automation;

[ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class DeviceEnumerator {}
[ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IDeviceEnumerator {
 void EnumAudioEndpoints();
 void GetDefaultAudioEndpoint(int flow, int role, out IDevice device);
}
[ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IDevice {
 void Activate(ref Guid iid, int context, IntPtr parameters, [MarshalAs(UnmanagedType.IUnknown)] out object result);
 void OpenPropertyStore(int access, out IPropertyStore store);
}
[ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IEndpoint {
 void Register(); void Unregister(); void Channels();
 void SetDb(); void SetScalar(); void GetDb(); void GetScalar();
 void SetChannelDb(); void SetChannelScalar(); void GetChannelDb(); void GetChannelScalar();
 void SetMute(); void GetMute([MarshalAs(UnmanagedType.Bool)] out bool muted);
 void GetStep(); void StepUp(); void StepDown();
 void QueryHardwareSupport(out uint mask);
}
[StructLayout(LayoutKind.Sequential)]
struct PropertyKey {
 public Guid Format;
 public uint Id;
}

[StructLayout(LayoutKind.Explicit, Size = 24)]
struct PropertyValue {
 [FieldOffset(0)] public ushort Type;
 [FieldOffset(8)] public IntPtr Pointer;
}

[ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IPropertyStore {
 void GetCount();
 void GetAt();
 void GetValue(ref PropertyKey key, out PropertyValue value);
}

static class SeirenDiscord {
 static readonly string DiscordRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord") + Path.DirectorySeparatorChar;
 static readonly string LogFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SeirenDiscord", "SeirenDiscord.log");

 static void Log(string message) {
  try {
   Directory.CreateDirectory(Path.GetDirectoryName(LogFile));
   File.AppendAllText(LogFile, DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine);
  } catch (IOException) {} catch (UnauthorizedAccessException) {}
 }

 static IEndpoint OpenEndpoint() {
  var enumerator = (IDeviceEnumerator)new DeviceEnumerator();
  IDevice device = null;
  object instance;
  Guid iid = typeof(IEndpoint).GUID;
  try {
   enumerator.GetDefaultAudioEndpoint(1, 1, out device);
   if (!IsSeiren(device)) return null;
   device.Activate(ref iid, 23, IntPtr.Zero, out instance);
   return (IEndpoint)instance;
  } finally {
   if (device != null) Marshal.ReleaseComObject(device);
   Marshal.ReleaseComObject(enumerator);
  }
 }

 [DllImport("ole32.dll")]
 static extern int PropVariantClear(ref PropertyValue value);

 static bool IsSeiren(IDevice device) {
  IPropertyStore store;
  device.OpenPropertyStore(0, out store);
  var key = new PropertyKey { Format = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), Id = 14 };
  PropertyValue value = default(PropertyValue);
  try {
   store.GetValue(ref key, out value);
   string name = value.Type == 31 ? Marshal.PtrToStringUni(value.Pointer) : null;
   return name != null && name.IndexOf("Razer Seiren V3 Chroma", StringComparison.OrdinalIgnoreCase) >= 0;
  } finally {
   PropVariantClear(ref value);
   Marshal.ReleaseComObject(store);
  }
 }

 static TogglePattern FindSwitch(AutomationElement window, string name) {
  var matches = window.FindAll(TreeScope.Descendants, new AndCondition(
   new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
   new PropertyCondition(AutomationElement.NameProperty, name),
   new PropertyCondition(AutomationElement.IsTogglePatternAvailableProperty, true)));
  if (matches.Count != 1 || !matches[0].Current.IsEnabled) return null;
  return (TogglePattern)matches[0].GetCurrentPattern(TogglePattern.Pattern);
 }

 static AutomationElement FindDiscord() {
  AutomationElement result = null;
  foreach (var process in Process.GetProcessesByName("Discord")) {
   using (process) {
    if (process.MainWindowHandle == IntPtr.Zero) continue;
    if (!process.MainModule.FileName.StartsWith(DiscordRoot, StringComparison.OrdinalIgnoreCase)) continue;
    if (result != null) return null;
    result = AutomationElement.FromHandle(process.MainWindowHandle);
   }
  }
  return result;
 }

 static void Sync(bool muted) {
  try {
   var window = FindDiscord();
   var mic = window == null ? null : FindSwitch(window, "Mute");
   var deafen = window == null ? null : FindSwitch(window, "Deafen");
   if (mic == null || deafen == null) {
    Log("Discord controls unavailable");
    return;
   }
   var state = mic.Current.ToggleState;
   var target = muted ? ToggleState.On : ToggleState.Off;
   if (state == target) {
    Log("Discord already in sync (" + target + ")");
    return;
   }
   if (state == ToggleState.Indeterminate || deafen.Current.ToggleState != ToggleState.Off) {
    Log("Discord is deafened or its mute state is uncertain. Skipping change.");
    return;
   }
   mic.Toggle();
   for (int i = 0; i < 8; i++) {
    Thread.Sleep(100);
    if (mic.Current.ToggleState == target) {
     Log("Discord mute state verified (" + target + ")");
     return;
    }
   }
   Log("Discord: mute state change could not be verified");
  } catch (Exception e) {
   Log("Discord: " + e.GetType().Name + " " + e.Message);
  }
 }

 [STAThread] static void Main() {
  using (var mutex = new Mutex(false, @"Local\SeirenDiscordMuteBridge")) {
   try {
    if (!mutex.WaitOne(0)) return;
   } catch (AbandonedMutexException) {}
   Log("Bridge started");
   IEndpoint endpoint = null;
   bool? previous = null;
   while (true) {
    try {
     if (endpoint == null) endpoint = OpenEndpoint();
     if (endpoint == null) { Thread.Sleep(1000); continue; }
     bool muted;
     endpoint.GetMute(out muted);
     if (previous != muted) {
      Log("Seiren muted=" + muted);
      if (previous.HasValue || muted) Sync(muted);
      previous = muted;
     }
     Thread.Sleep(125);
    } catch (COMException e) {
     if (endpoint != null) Marshal.ReleaseComObject(endpoint);
     if (endpoint != null || previous.HasValue) Log("Core Audio unavailable: " + e.ErrorCode);
     endpoint = null;
     previous = null;
     Thread.Sleep(1000);
    }
   }
  }
 }
}
