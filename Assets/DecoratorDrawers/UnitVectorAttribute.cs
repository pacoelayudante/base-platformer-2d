using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UnitVectorAttribute : PropertyAttribute
{
    // la parte de ser UNIT tendria que ser la bool, sino es solo un vec 2 controlado por angulo
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(UnitVectorAttribute))]
    public class UnitVectorAttributeEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Vector2)
            {
                position.height -= EditorGUIUtility.singleLineHeight;
                position.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(position, property);
                position.height = EditorGUIUtility.singleLineHeight;
                position.y -= EditorGUIUtility.singleLineHeight;
                EditorGUI.HelpBox(position, $"{nameof(UnitVectorAttribute)} is not compatible with properties of type {property.propertyType}", MessageType.Warning);
            }
            else
            {
                Rect wheelControl = new Rect(position.position, EditorGUIUtility.singleLineHeight * 2f * Vector2.one);
                position.x += wheelControl.width;
                position.width -= wheelControl.width;

                EditorGUI.DrawRect(wheelControl, Color.black * .1f);
                if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
                    && Event.current.button == 0
                    && wheelControl.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    var difVec = (Event.current.mousePosition - wheelControl.center).normalized;
                    difVec.y *= -1f;
                    property.vector2Value = difVec;
                }

                position.height -= EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(position, property, label); // NO ESTA ENFORCED eL UNIT VECTOR ACA
                position.y += position.height;
                position.height = EditorGUIUtility.singleLineHeight;

                using (new EditorGUI.PropertyScope(position, label, property))
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    var angulo = Vector2.SignedAngle(Vector2.right, property.vector2Value);
                    angulo = EditorGUI.Slider(position, new GUIContent("Angle", "Angle from a right pointing vector to this unit vector"), angulo, -180f, 180f);
                    if (change.changed)
                    {
                        if (angulo % 90f == 0) // para angulos rectos mejor setear a mano
                        {
                            var x = (angulo % 180 == 0 ? 1 : 0) * (angulo % 360 == 0f ? 1 : -1);
                            var y = (angulo % 180 == 0 ? 0 : 1) * (angulo / 90);
                            property.vector2Value = new Vector2(x, y);// * magnitud original
                        }
                        else
                        {
                            property.vector2Value = Quaternion.Euler(0f, 0f, angulo) * Vector2.right;
                        }
                    }
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label)
                + (EditorGUIUtility.wideMode ? 0f : EditorGUIUtility.singleLineHeight)
                + EditorGUIUtility.singleLineHeight;
        }
    }
#endif
}
