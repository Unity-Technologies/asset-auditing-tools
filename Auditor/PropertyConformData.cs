using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AssetTools
{

	public class PropertyConformData
	{
		public bool m_Conforms = true;

		public bool Conforms
		{
			get
			{
				if( assetSerializedProperty.propertyType == SerializedPropertyType.Generic )
				{
					for( int i = 0; i < subData.Count; ++i )
					{
						if( !subData[i].Conforms )
							return false;
					}
				}
				return m_Conforms;
			}
			set { m_Conforms = value; }
		}
		public string propertyName;

		public List<PropertyConformData> subData = new List<PropertyConformData>();
		
		internal SerializedProperty templateSerializedProperty;
		internal SerializedProperty assetSerializedProperty;
		

		public PropertyConformData( string name )
		{
			propertyName = name;
		}

		public void SetSerializedProperties( SerializedProperty template, SerializedProperty asset )
		{
			templateSerializedProperty = template;
			assetSerializedProperty = asset;
			Conforms = CompareSerializedProperty( asset, template );
		}

		public string TemplateValue
		{
			get { return GetValue( templateSerializedProperty ); }
		}
		
		public SerializedPropertyType TemplateType
		{
			get { return templateSerializedProperty.propertyType; }
		}

		public string AssetValue
		{
			get { return GetValue( assetSerializedProperty ); }
		}

		string GetValue( SerializedProperty property )
		{
			if( property == null )
				return "";

			switch( property.propertyType )
			{
				case SerializedPropertyType.Generic:
					return "Generic";
				case SerializedPropertyType.Integer:
					return property.intValue.ToString();
				case SerializedPropertyType.Boolean:
					return property.boolValue.ToString();
				case SerializedPropertyType.Float:
					return property.floatValue.ToString();
				case SerializedPropertyType.String:
					return property.stringValue;
				case SerializedPropertyType.Color:
					return property.colorValue.ToString();
				case SerializedPropertyType.ObjectReference:
					return "Object"; // TODO this is weird on models imports and needs a solution as the exposed transforms reference the model itself
				case SerializedPropertyType.LayerMask:
					break;
				case SerializedPropertyType.Enum:
					return property.enumValueIndex.ToString();
				case SerializedPropertyType.Vector2:
					return property.vector2Value.ToString();
				case SerializedPropertyType.Vector3:
					return property.vector3Value.ToString();
				case SerializedPropertyType.Vector4:
					return property.vector4Value.ToString();
				case SerializedPropertyType.Rect:
					return property.rectValue.ToString();
				case SerializedPropertyType.ArraySize:
					return "arraySize";
				case SerializedPropertyType.Character:
					Debug.Log( "something is a character, what is this?" );
					break;
				case SerializedPropertyType.AnimationCurve:
					return "AnimationCurve";
				case SerializedPropertyType.Bounds:
					return property.boundsValue.ToString();
				case SerializedPropertyType.Gradient:
					break;
				case SerializedPropertyType.Quaternion:
					return property.quaternionValue.ToString();
				case SerializedPropertyType.ExposedReference:
					return property.exposedReferenceValue.ToString();
#if UNITY_2017_1_OR_NEWER
				case SerializedPropertyType.FixedBufferSize:
					break;
#endif
				default:
					throw new ArgumentOutOfRangeException();
			}

			return "";
		}

		public bool CompareSerializedProperty( SerializedProperty baseAssetSP, SerializedProperty templateSp )
		{
			if( baseAssetSP.propertyPath == "m_FileIDToRecycleName" )
				return true; // the file ids will always be different so we should skip over this.

			switch( baseAssetSP.propertyType )
			{
				case SerializedPropertyType.Generic: // this eventually goes down through the data until we get a useable value to compare 

					SerializedProperty assetSPTarget = baseAssetSP.Copy();
					SerializedProperty templateSPTarget = templateSp.Copy();

					// we must get the next sibling SerializedProperties to know when to stop the comparison
					SerializedProperty assetSiblingSP = baseAssetSP.Copy();
					SerializedProperty templateSiblineSP = templateSp.Copy();
					assetSiblingSP.NextVisible( false );
					templateSiblineSP.NextVisible( false );

					bool asset, found;
					bool enterChildren = true;

					do
					{
						if( templateSPTarget.propertyType != assetSPTarget.propertyType )
						{
							return false; // mistmatch in types different serialisation
						}

						if( SerializedProperty.EqualContents( assetSPTarget, baseAssetSP ) == false )
						{
							PropertyConformData conformData = new PropertyConformData( templateSPTarget.name );
							conformData.SetSerializedProperties( templateSPTarget.Copy(), assetSPTarget.Copy() );
							
							subData.Add( conformData );

							if( templateSPTarget.propertyType != SerializedPropertyType.Generic )
							{
								CompareSerializedProperty( assetSPTarget, templateSPTarget );
							}
						}

						asset = assetSPTarget.NextVisible( enterChildren );
						found = templateSPTarget.NextVisible( enterChildren );
						enterChildren = false; // only enter first child, then only siblings
					} while( found && asset &&
					         // once it hits a sibline, end
					         !SerializedProperty.EqualContents( assetSPTarget, assetSiblingSP ) &&
					         !SerializedProperty.EqualContents( templateSPTarget, templateSiblineSP ) );

					return true;
				case SerializedPropertyType.Integer:
					return baseAssetSP.intValue == templateSp.intValue;
				case SerializedPropertyType.Boolean:
					return baseAssetSP.boolValue == templateSp.boolValue;
				case SerializedPropertyType.Float:
					return Mathf.Approximately( baseAssetSP.floatValue, templateSp.floatValue );
				case SerializedPropertyType.String:
					return baseAssetSP.stringValue == templateSp.stringValue;
				case SerializedPropertyType.Color:
					return baseAssetSP.colorValue == templateSp.colorValue;
				case SerializedPropertyType.ObjectReference:
					return true; // this is weird on models imports and needs a solution as the exposed transforms reference the model itself
				case SerializedPropertyType.LayerMask:
					break;
				case SerializedPropertyType.Enum:
					return baseAssetSP.enumValueIndex == templateSp.enumValueIndex;
				case SerializedPropertyType.Vector2:
					return baseAssetSP.vector2Value == templateSp.vector2Value;
				case SerializedPropertyType.Vector3:
					return baseAssetSP.vector3Value == templateSp.vector3Value;
				case SerializedPropertyType.Vector4:
					return baseAssetSP.vector4Value == templateSp.vector4Value;
				case SerializedPropertyType.Rect:
					return baseAssetSP.rectValue == templateSp.rectValue;
				case SerializedPropertyType.ArraySize:
					if( baseAssetSP.isArray && templateSp.isArray )
						return baseAssetSP.arraySize == templateSp.arraySize;
					else
						return baseAssetSP.intValue == templateSp.intValue;
				case SerializedPropertyType.Character:
					Debug.Log( "something is a character, what is this?" );
					break;
				case SerializedPropertyType.AnimationCurve:
					return baseAssetSP.animationCurveValue == templateSp.animationCurveValue;
				case SerializedPropertyType.Bounds:
					return baseAssetSP.boundsValue == templateSp.boundsValue;
				case SerializedPropertyType.Gradient:
					break;
				case SerializedPropertyType.Quaternion:
					return baseAssetSP.quaternionValue == templateSp.quaternionValue;
				case SerializedPropertyType.ExposedReference:
					return baseAssetSP.exposedReferenceValue == templateSp.exposedReferenceValue;
#if UNITY_2017_1_OR_NEWER
				case SerializedPropertyType.FixedBufferSize:
					break;
#endif
				default:
					throw new ArgumentOutOfRangeException();
			}

			return false;
		}
	}
}