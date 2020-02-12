using UnityEngine;
using System;   // for AttributeUsage, AttributeTargets
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SouthernForge.Utils
{
    #region inspector bit mask
    // taken from https://answers.unity.com/questions/486694/default-editor-enum-as-flags-.html
    public class EnumFlags : PropertyAttribute {
		public EnumFlags() { }
    }
    #endregion

    #region inspector read only attributes
    // taken from https://answers.unity.com/questions/489942/how-to-make-a-readonly-property-in-inspector.html
    public class ReadOnlyAttribute : PropertyAttribute
    {
        public ReadOnlyAttribute() { }
    }
    public class BeginReadOnlyGroupAttribute : PropertyAttribute { }
    public class EndReadOnlyGroupAttribute : PropertyAttribute { }
    #endregion

    #region conditional hide attribute
    // taken from http://www.brechtos.com/hiding-or-disabling-inspector-properties-using-propertydrawers-within-unity-5/
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class ConditionalHideAttribute : PropertyAttribute
    {
        //The name of the bool field that will be in control
        public string ConditionalSourceField = "";
        //TRUE = Hide in inspector / FALSE = Disable in inspector 
        public bool HideInInspector = false;
     
        public ConditionalHideAttribute(string conditionalSourceField)
        {
            this.ConditionalSourceField = conditionalSourceField;
            this.HideInInspector = false;
        }
     
        public ConditionalHideAttribute(string conditionalSourceField, bool hideInInspector)
        {
            this.ConditionalSourceField = conditionalSourceField;
            this.HideInInspector = hideInInspector;
        }
    }
    #endregion

    #region displayGridMatrix attribute
    public class DisplayGridMatrixAttribute : PropertyAttribute
    {
        public bool isReadOnly = false;

        public DisplayGridMatrixAttribute(bool isReadOnly = false)
        {
            this.isReadOnly = isReadOnly;
        }

    }
    #endregion displayGridMatrix attribute


#if UNITY_EDITOR
    [CustomPropertyDrawer( typeof( EnumFlags ) )]
	public class EnumFlagsPropertyDrawer : PropertyDrawer
    {
		public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
		{
			property.intValue = EditorGUI.MaskField( position, label, property.intValue, property.enumNames );
		}
    }

	[CustomPropertyDrawer( typeof( ReadOnlyAttribute ) )]
	public class ReadOnlyPropertyDrawer : PropertyDrawer
    {
		public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
		{
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
		}
    }

    [CustomPropertyDrawer( typeof( BeginReadOnlyGroupAttribute ) )]
    public class BeginReadOnlyGroupDrawer : DecoratorDrawer {
 
        public override float GetHeight() { return 0; }
 
        public override void OnGUI( Rect position ) {
            EditorGUI.BeginDisabledGroup( true );
        }
    }

    [CustomPropertyDrawer( typeof( EndReadOnlyGroupAttribute ) )]
    public class EndReadOnlyGroupDrawer : DecoratorDrawer {

        public override float GetHeight() { return 0; }

        public override void OnGUI( Rect position ) {
            EditorGUI.EndDisabledGroup();
        }
 
    }

    [CustomPropertyDrawer( typeof( ConditionalHideAttribute ) )]
    public class ConditionalHidePropertyDrawer : PropertyDrawer {

        // public override float GetHeight() { return 0; }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
          {
              ConditionalHideAttribute condHAtt = (ConditionalHideAttribute)attribute;
              bool enabled = GetConditionalHideAttributeResult(condHAtt, property);
         
              if (!condHAtt.HideInInspector || enabled)
              {
                  return EditorGUI.GetPropertyHeight(property, label);
              }
              else
              {
                  //The property is not being drawn
                  //We want to undo the spacing added before and after the property
                  return -EditorGUIUtility.standardVerticalSpacing;
              }
          }

		public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
		{
            //get the attribute data
            ConditionalHideAttribute condHAtt = (ConditionalHideAttribute)attribute;
            //check if the propery we want to draw should be enabled
            bool enabled = GetConditionalHideAttributeResult(condHAtt, property);

            //Enable/disable the property
            bool wasEnabled = GUI.enabled;
            GUI.enabled = enabled;

            //Check if we should draw the property
            if (!condHAtt.HideInInspector || enabled)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }

            //Ensure that the next property that is being drawn uses the correct settings
            GUI.enabled = wasEnabled;
		}

        private bool GetConditionalHideAttributeResult(ConditionalHideAttribute condHAtt, SerializedProperty property)
        {
            bool enabled = true;
            //Look for the sourcefield within the object that the property belongs to
             string propertyPath = property.propertyPath; //returns the property path of the property we want to apply the attribute to
            string conditionPath = propertyPath.Replace(property.name, condHAtt.ConditionalSourceField); //changes the path to the conditionalsource property path
            SerializedProperty sourcePropertyValue = property.serializedObject.FindProperty(conditionPath);
         
            if (sourcePropertyValue != null)
            {
                enabled = sourcePropertyValue.boolValue;
            }
            else
            {
                Debug.LogWarning("Attempting to use a ConditionalHideAttribute but no matching SourcePropertyValue found in object: " + condHAtt.ConditionalSourceField);
            }
         
            return enabled;
        }
 
    }

    [CustomPropertyDrawer( typeof( DisplayGridMatrixAttribute ) )]
    public class DisplayGridMatrixDrawer : PropertyDrawer {

        // public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        // {
        //     return EditorGUI.GetPropertyHeight(property, label);
        // }

        private bool foldout = false;


		public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
		{
            DisplayGridMatrixAttribute displayGridMatrixAttrib = attribute as DisplayGridMatrixAttribute;
            if (displayGridMatrixAttrib.isReadOnly) GUI.enabled = false;

            SerializedProperty e00Property = property.FindPropertyRelative("e00");
            SerializedProperty e01Property = property.FindPropertyRelative("e01");
            SerializedProperty e02Property = property.FindPropertyRelative("e02");
            SerializedProperty e03Property = property.FindPropertyRelative("e03");

            SerializedProperty e10Property = property.FindPropertyRelative("e10");
            SerializedProperty e11Property = property.FindPropertyRelative("e11");
            SerializedProperty e12Property = property.FindPropertyRelative("e12");
            SerializedProperty e13Property = property.FindPropertyRelative("e13");

            SerializedProperty e20Property = property.FindPropertyRelative("e20");
            SerializedProperty e21Property = property.FindPropertyRelative("e21");
            SerializedProperty e22Property = property.FindPropertyRelative("e22");
            SerializedProperty e23Property = property.FindPropertyRelative("e23");

            SerializedProperty e30Property = property.FindPropertyRelative("e30");
            SerializedProperty e31Property = property.FindPropertyRelative("e31");
            SerializedProperty e32Property = property.FindPropertyRelative("e32");
            SerializedProperty e33Property = property.FindPropertyRelative("e33");

            foldout = EditorGUILayout.Foldout(foldout, label);
            if (foldout)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.FloatField(e00Property.floatValue);
                EditorGUILayout.FloatField(e01Property.floatValue);
                EditorGUILayout.FloatField(e02Property.floatValue);
                EditorGUILayout.FloatField(e03Property.floatValue);
                EditorGUILayout.EndHorizontal();        

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.FloatField(e10Property.floatValue);
                EditorGUILayout.FloatField(e11Property.floatValue);
                EditorGUILayout.FloatField(e12Property.floatValue);
                EditorGUILayout.FloatField(e13Property.floatValue);
                EditorGUILayout.EndHorizontal();        

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.FloatField(e20Property.floatValue);
                EditorGUILayout.FloatField(e21Property.floatValue);
                EditorGUILayout.FloatField(e22Property.floatValue);
                EditorGUILayout.FloatField(e23Property.floatValue);
                EditorGUILayout.EndHorizontal();        

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.FloatField(e30Property.floatValue);
                EditorGUILayout.FloatField(e31Property.floatValue);
                EditorGUILayout.FloatField(e32Property.floatValue);
                EditorGUILayout.FloatField(e33Property.floatValue);
                EditorGUILayout.EndHorizontal();        
            }

            if (displayGridMatrixAttrib.isReadOnly) GUI.enabled = true;
		}
    }
#endif
}

