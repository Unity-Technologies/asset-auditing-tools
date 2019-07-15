using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetTools.GUIUtility
{
	public static class SerializationUtilities
	{
		public static List<SerializedProperty> GetSerialisedPropertyCopiesForObject( SerializedProperty classProp, IList<string> targets )
		{
			SerializedProperty copy = classProp.Copy();
			List<SerializedProperty> properties = new List<SerializedProperty>(targets.Count);
			for( int i=0; i<targets.Count; ++i )
				properties.Add( null );
			copy.NextVisible( true );
			do
			{
				for( int i = 0; i < targets.Count; ++i )
				{
					if( targets[i].Equals( copy.name ) )
						properties[i] = copy.Copy();
				}
			} while( copy.NextVisible(  false ) );

			return properties;
		}
		
		public static List<SerializedProperty> GetSerialisedPropertyCopiesForObject( SerializedObject classObj, IList<string> targets )
		{
			SerializedProperty copy = classObj.GetIterator().Copy();
			List<SerializedProperty> properties = new List<SerializedProperty>(targets.Count);
			for( int i=0; i<targets.Count; ++i )
				properties.Add( null );
			copy.NextVisible( true );
			do
			{
				for( int i = 0; i < targets.Count; ++i )
				{
					if( targets[i].Equals( copy.name ) )
						properties[i] = copy.Copy();
				}
			} while( copy.NextVisible(  false ) );

			return properties;
		}
	}

	public class ControlRect
	{
		private float x = 0;
		private float width = 0;

		private float currentY = 0;
		private Rect lastRect = Rect.zero;

		public readonly float layoutHeight = 16;
		public float padding = 3;

		private float areaXPadding = 0;
		private float areaYPadding = 0;

		private float areaStartY = 0;

		public Rect LastRect
		{
			get { return new Rect( lastRect ); }
		}

		public float FullHeight
		{
			get { return layoutHeight + padding; }
		}

		public ControlRect( float x, float y, float width )
		{
			this.x = x;
			currentY = y;
			this.width = width;
		}

		public ControlRect( float x, float y, float width, float layoutHeight )
		{
			this.x = x;
			currentY = y;
			this.width = width;
			this.layoutHeight = layoutHeight;
		}

		public Rect Get()
		{
			Rect r = new Rect( x + areaXPadding, currentY, width - (areaXPadding*2), layoutHeight );
			lastRect.Set( x + areaXPadding, currentY, width - (areaXPadding*2), layoutHeight );
			currentY += layoutHeight + padding;
			return r;
		}

		public Rect Get( float height )
		{
			Rect r = new Rect( x + areaXPadding, currentY, width - (areaXPadding*2), height );
			lastRect.Set( x + areaXPadding, currentY, width - (areaXPadding*2), height );
			currentY += height + padding;
			return r;
		}

		public void Space( float space )
		{
			currentY += space;
		}

		public void BeginArea( float xPadding, float yPadding )
		{
			if( areaStartY > 1 )
			{
				Debug.LogError( "Cannot begin area multiple times" );
				return;
			}
			areaStartY = currentY;
			areaXPadding = xPadding;
			areaYPadding = yPadding;
			Space( areaYPadding );
		}

		public Rect EndArea()
		{
			Space( areaYPadding );
			Rect r = new Rect(x, areaStartY, width, currentY-areaStartY);
			
			areaStartY = 0;
			areaXPadding = 0;
			areaYPadding = 0;
			return r;
		}
	}

}