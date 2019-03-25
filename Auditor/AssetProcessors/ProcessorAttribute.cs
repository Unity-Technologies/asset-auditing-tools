using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AssetTools
{
	[AttributeUsage(AttributeTargets.Method)]
	public class ProcessorAttribute : Attribute
	{
		private int versionNumber = 0;
		public ProcessorAttribute( int versionNumber )
		{
			this.versionNumber = versionNumber;
		}
	}
}