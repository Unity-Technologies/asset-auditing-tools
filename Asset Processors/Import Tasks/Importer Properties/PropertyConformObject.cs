using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEditor;

namespace AssetTools
{

	public class PropertyConformObject : IConformObject
	{
		public bool Conforms
		{
			get
			{
				if( m_AssetSerializedProperty.propertyType != SerializedPropertyType.Generic )
					return m_Conforms;
				
				for( int i = 0; i < m_SubObjects.Count; ++i )
				{
					if( !m_SubObjects[i].Conforms )
						return false;
				}
				return m_Conforms;
			}
			set { m_Conforms = value; }
		}

		public List<IConformObject> SubObjects
		{
			get { return m_SubObjects; }
			set { m_SubObjects = value; }
		}

		public string Name
		{
			get { return m_PropertyName; }
			set { m_PropertyName = value; }
		}

		private bool m_Conforms = true;
		private List<IConformObject> m_SubObjects = new List<IConformObject>();
		private string m_PropertyName;

		private SerializedProperty m_TemplateSerializedProperty;
		private SerializedProperty m_AssetSerializedProperty;
		
		public PropertyConformObject( string name )
		{
			m_PropertyName = name;
		}

		private PropertyConformObject( string name, SerializedProperty template, SerializedProperty asset )
		{
			m_PropertyName = name;
			SetSerializedProperties( template, asset );
		}

		public void SetSerializedProperties( SerializedProperty template, SerializedProperty asset )
		{
			m_TemplateSerializedProperty = template;
			m_AssetSerializedProperty = asset;
			Conforms = CompareSerializedProperty( asset, template );
		}

		public SerializedProperty TemplateSerializedProperty
		{
			get { return m_TemplateSerializedProperty; }
		}
		
		// TODO cache these values??
		public string TemplateValue
		{
			get { return GetValue( m_TemplateSerializedProperty ); }
		}

		public string AssetValue
		{
			get { return GetValue( m_AssetSerializedProperty ); }
		}

		public SerializedPropertyType TemplateType
		{
			get { return m_TemplateSerializedProperty.propertyType; }
		}

		public SerializedProperty AssetSerializedProperty
		{
			get { return m_AssetSerializedProperty; }
		}

		public string ActualValue
		{
			get
			{
				if( AssetSerializedProperty.propertyType != SerializedPropertyType.Generic )
					return AssetValue;
				return "";
			}
		}

		public string ExpectedValue
		{
			get
			{ 
				if( AssetSerializedProperty.propertyType != SerializedPropertyType.Generic )
					return TemplateValue;
				return "";
			}
		}

		public bool Apply( SerializedObject toObject )
		{
			toObject.CopyFromSerializedProperty( TemplateSerializedProperty );
			if( !toObject.ApplyModifiedProperties() )
			{
				Debug.LogError( "Copying of SerialisedProperty failed for - " + toObject.targetObject.name );
				return false;
			}

			return true;
		}
		
		public void AddTreeViewItems( int parentId, ConformObjectTreeViewItem parent, AssetsTreeViewItem assetsTreeItem, int depth, int arrayIndex = -1 )
		{
			string extra = arrayIndex >= 0 ? arrayIndex.ToString() : "";
			int hashCodeForID = parentId + (Name + extra).GetHashCode() * 31;
			ConformObjectTreeViewItem conformObjectTree = new ConformObjectTreeViewItem( hashCodeForID, depth, this )
			{
				AssetsTreeViewItem = assetsTreeItem
			};
			parent.AddChild( conformObjectTree );

			for( int i=0; i<SubObjects.Count; ++i )
			{
				// TODO will this be slow? , need to see if there is a better way to cache object type
				if( SubObjects[i] is PropertyConformObject )
					SubObjects[i].AddTreeViewItems( hashCodeForID, conformObjectTree, assetsTreeItem, depth+1, AssetSerializedProperty.isArray ? i : -1 );
			}
		}

		private static string GetValue( SerializedProperty property )
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
					return property.floatValue.ToString( CultureInfo.CurrentCulture );
				case SerializedPropertyType.String:
					return property.stringValue;
				case SerializedPropertyType.Color:
					return property.colorValue.ToString();
				case SerializedPropertyType.ObjectReference:
					// TODO this is weird on models imports and needs a solution as the exposed transforms reference the model itself
					return "Object";
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

		private bool CompareSerializedProperty( SerializedProperty baseAssetSP, SerializedProperty templateSp )
		{
			if( baseAssetSP.propertyPath == "m_FileIDToRecycleName" )
				return true; // the file ids will always be different so we should skip over this.

			switch( baseAssetSP.propertyType )
			{
				case SerializedPropertyType.Generic: // this eventually goes down through the @object until we get a usable value to compare 

					SerializedProperty assetSPTarget = baseAssetSP.Copy();
					SerializedProperty templateSPTarget = templateSp.Copy();

					// we must get the next sibling SerializedProperties to know when to stop the comparison
					SerializedProperty assetSiblingSP = baseAssetSP.Copy();
					SerializedProperty templateSiblingSP = templateSp.Copy();
					assetSiblingSP.NextVisible( false );
					templateSiblingSP.NextVisible( false );

					bool asset, found;
					bool enterChildren = true;

					do
					{
						if( templateSPTarget.propertyType != assetSPTarget.propertyType )
						{
							return false; // mismatch in types different serialisation
						}

						if( SerializedProperty.EqualContents( assetSPTarget, baseAssetSP ) == false )
						{
							m_SubObjects.Add( new PropertyConformObject( templateSPTarget.name, templateSPTarget.Copy(), assetSPTarget.Copy() ) );

							if( templateSPTarget.propertyType != SerializedPropertyType.Generic )
							{
								CompareSerializedProperty( assetSPTarget, templateSPTarget );
							}
						}

						asset = assetSPTarget.NextVisible( enterChildren );
						found = templateSPTarget.NextVisible( enterChildren );
						enterChildren = false; // only enter first child, then only siblings
					} while( found && asset &&
					         // once it hits a sibling, end
					         !SerializedProperty.EqualContents( assetSPTarget, assetSiblingSP ) &&
					         !SerializedProperty.EqualContents( templateSPTarget, templateSiblingSP ) );

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
					return true; // TODO this is weird on models imports and needs a solution as the exposed transforms reference the model itself
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