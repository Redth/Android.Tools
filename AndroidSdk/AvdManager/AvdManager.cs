﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace AndroidSdk
{
	public partial class AvdManager : SdkTool
	{
		public AvdManager()
			: this((DirectoryInfo)null)
		{ }

		public AvdManager(DirectoryInfo androidSdkHome)
			: base(androidSdkHome)
		{
			AndroidSdkHome = androidSdkHome;
		}

		public AvdManager(string androidSdkHome)
			: this(new DirectoryInfo(androidSdkHome))
		{ }

		internal override string SdkPackageId => "emulator";

		public override FileInfo FindToolPath(DirectoryInfo androidSdkHome)
			=> FindTool(androidSdkHome, toolName: "avdmanager", windowsExtension: ".bat", "tools", "bin");

		public void Create(string name, string sdkId, string device = null, string path = null, bool force = false)
		{
			var args = new List<string> {
				"create", "avd", "-n", name, "-k", $"\"{sdkId}\""
			};

			if (!string.IsNullOrEmpty(device))
			{
				args.Add("--device");
				args.Add($"\"{device}\"");
			}

			if (!string.IsNullOrEmpty(path))
			{
				args.Add("-c");
				args.Add($"\"{path}\"");
			}

			if (force)
				args.Add("--force");

			if (!string.IsNullOrEmpty(path))
			{
				args.Add("-p");
				args.Add($"\"{path}\"");
			}

			run(args.ToArray());
		}

		public void Delete(string name)
		{
			run("delete", "avd", "-n", name);
		}

		public void Move(string name, string path = null, string newName = null)
		{
			var args = new List<string> {
				"move", "avd", "-n", name
			};

			if (!string.IsNullOrEmpty(path))
			{
				args.Add("-p");
				args.Add(path);
			}

			if (!string.IsNullOrEmpty(newName))
			{
				args.Add("-r");
				args.Add(newName);
			}

			run(args.ToArray());
		}

		static Regex rxListTargets = new Regex(@"id:\s+(?<id>[^\n]+)\s+Name:\s+(?<name>[^\n]+)\s+Type\s?:\s+(?<type>[^\n]+)\s+API level\s?:\s+(?<api>[^\n]+)\s+Revision\s?:\s+(?<revision>[^\n]+)", RegexOptions.Multiline | RegexOptions.Compiled);

		public IEnumerable<AvdTarget> ListTargets()
		{
			var r = new List<AvdTarget>();

			var lines = run("list", "target");

			var str = string.Join("\n", lines);

			var matches = rxListTargets.Matches(str);
			if (matches != null && matches.Count > 0)
			{
				foreach (Match m in matches)
				{
					var a = new AvdTarget
					{
						Name = m.Groups?["name"]?.Value,
						Id = m.Groups?["id"]?.Value,
						Type = m.Groups?["type"]?.Value
					};

					if (int.TryParse(m.Groups?["api"]?.Value, out var api))
						a.ApiLevel = api;
					if (int.TryParse(m.Groups?["revision"]?.Value, out var rev))
						a.Revision = rev;

					if (!string.IsNullOrWhiteSpace(a.Id) && a.ApiLevel > 0)
						r.Add(a);
				}
			}

			return r;
		}

		static Regex rxListAvds = new Regex(@"\s+Name:\s+(?<name>[^\n]+)\s+Device:\s+(?<device>[^\n]+)\s+Path:\s+(?<path>[^\n]+)\s+Target:\s+(?<target>[^\n]+)\s+Based on:\s+(?<basedon>[^\n]+)", RegexOptions.Compiled | RegexOptions.Multiline);
		public IEnumerable<Avd> ListAvds()
		{
			var r = new List<Avd>();

			var lines = run("list", "avd");

			var str = string.Join("\n", lines);

			var matches = rxListAvds.Matches(str);
			if (matches != null && matches.Count > 0)
			{
				foreach (Match m in matches)
				{
					var a = new Avd
					{
						Name = m.Groups?["name"]?.Value,
						Device = m.Groups?["device"]?.Value,
						Path = m.Groups?["path"]?.Value,
						Target = m.Groups?["target"]?.Value,
						BasedOn = m.Groups?["basedon"]?.Value
					};

					if (!string.IsNullOrWhiteSpace(a.Name))
						r.Add(a);
				}
			}

			return r;
		}

		static Regex rxListDevices = new Regex(@"id:\s+(?<id>[^\n]+)\s+Name:\s+(?<name>[^\n]+)\s+OEM\s?:\s+(?<oem>[^\n]+)", RegexOptions.Singleline | RegexOptions.Compiled);

		public IEnumerable<AvdDevice> ListDevices()
		{
			var r = new List<AvdDevice>();

			var lines = run("list", "device");

			var str = string.Join("\n", lines);

			var matches = rxListDevices.Matches(str);
			if (matches != null && matches.Count > 0)
			{
				foreach (Match m in matches)
				{
					var a = new AvdDevice
					{
						Name = m.Groups?["name"]?.Value,
						Id = m.Groups?["id"]?.Value,
						Oem = m.Groups?["oem"]?.Value
					};

					if (!string.IsNullOrWhiteSpace(a.Name))
						r.Add(a);
				}
			}

			return r;
		}

		IEnumerable<string> run(params string[] args)
		{
			var adbManager = FindToolPath(AndroidSdkHome);
			if (adbManager == null || !File.Exists(adbManager.FullName))
				throw new FileNotFoundException("Could not find avdmanager", adbManager?.FullName);

			var builder = new ProcessArgumentBuilder();

			foreach (var arg in args)
				builder.Append(arg);

			var p = new ProcessRunner(adbManager, builder);

			var r = p.WaitForExit();

			return r.StandardOutput;
		}
	}
}
