using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetTools.GUIUtility
{
	public static class SerializationUtilities
	{
		public static List<SerializedProperty> FindPropertiesInClass( SerializedProperty classProp, IList<string> targets )
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
	}

	public class ControlRect
	{
		private readonly float x = 0;
		private readonly float width = 0;

		private float currentY = 0;
		private Rect lastRect = Rect.zero;

		public readonly float layoutHeight = 16;
		public float padding = 3;

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
			Rect r = new Rect( x, currentY, width, layoutHeight );
			lastRect.Set( x, currentY, width, layoutHeight );
			currentY += layoutHeight + padding;
			return r;
		}

		public Rect Get( float height )
		{
			Rect r = new Rect( x, currentY, width, height );
			lastRect.Set( x, currentY, width, height );
			currentY += height + padding;
			return r;
		}

		public void Space( float space )
		{
			currentY += space;
		}
	}

}