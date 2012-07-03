﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace CrossUI.Runner.WPF
{
	sealed class AssemblyTester : IDisposable
	{
		readonly UI.MainWindow _window;
		readonly StackPanel _testPanel;
		readonly List<AssemblyTest> _tests = new List<AssemblyTest>();

		public AssemblyTester(UI.MainWindow window)
		{
			_window = window;
			_testPanel = _window.Tests;

			var config = Configuration.load();

			var newControl = new UI.AssemblyTestNewControl();
			newControl.AddTestButton.Click += addTest;
			_testPanel.Children.Add(newControl);

			foreach (var c in config.AssemblyTests)
			{
				addTest(c);
			}
		}

		public void Dispose()
		{
			foreach (var t in _tests)
				t.Dispose();	
		}

		void addTest(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog
			{
				Filter = ".NET Assemblies|*.exe;*.dll|All Files|*.*", 
			};

			bool? res = dialog.ShowDialog();
			if (res == null || !res.Value)
				return;

			var config = AssemblyTestConfiguration.create(dialog.FileName);
			addTest(config);
		}

		void addTest(AssemblyTestConfiguration config)
		{
			var control = new UI.AssemblyTestControl();
			var test = new AssemblyTest(config, control);

			var insertPos = _testPanel.Children.Count - 1;

			_testPanel.Children.Insert(insertPos, control);
			_tests.Add(test);
		}
	}
}