using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RandomObjectGenerator : MonoBehaviour
{
    public GameObject TargetObject;
    public int ObjectNumber = 0;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(RandomObjectGenerator))]
    public class RandomObjectGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            RandomObjectGenerator generator = (RandomObjectGenerator)target;
            if (GUILayout.Button("Generate Objects"))
            {
                generator.GenerateObjects();
            }
            if (GUILayout.Button("Remove All Objects"))
            {
                generator.RemoveAllObjects();
            }
        }
    }
#endif

    public void RemoveAllObjects()
    {
        int childCount = this.transform.childCount;

        Debug.Log(childCount);

        for(int i =0; i< childCount ;i++)
        {
            DestroyImmediate(this.transform.GetChild(0).gameObject);
        }
    }


    public void GenerateObjects()
    {
        // �� ���� Object�� �����ϰ� ��ġ�ϴ� �ڵ带 �ۼ��ϼ���.
        for (int i = 0; i < ObjectNumber; i++)
        {
            // ������ ��ġ�� �����մϴ�.
            Vector3 randomPosition = new Vector3(
                Random.Range(transform.position.x - transform.localScale.x * 0.5f, transform.position.x + transform.localScale.x * 0.5f),
                Random.Range(transform.position.y - transform.localScale.y * 0.5f, transform.position.y + transform.localScale.y * 0.5f),
                Random.Range(transform.position.z - transform.localScale.z * 0.5f, transform.position.z + transform.localScale.z * 0.5f)
            );

            // TargetObject�� �����ϰ� ������ ��ġ�� ��ġ�մϴ�.
            GameObject newObject = Instantiate(TargetObject, randomPosition, Quaternion.identity);
            newObject.transform.SetParent(this.transform);
        }
        
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Box ������ ���̵������ �׸��ϴ�.
        Handles.color = Color.yellow;

        Vector3 position = transform.position;
        Vector3 scale = transform.localScale;

        

        Matrix4x4 cubeTransform = Matrix4x4.TRS(position, transform.rotation, scale);
        using (new Handles.DrawingScope(cubeTransform))
        {
            Handles.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
#endif
}