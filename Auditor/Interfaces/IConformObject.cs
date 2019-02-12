using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AssetTools
{
	public interface IConformObject
	{
		string Name { get; set; }
		
		bool Conforms { get; set; }

		List<IConformObject> SubObjects { get; set; }
	}
}