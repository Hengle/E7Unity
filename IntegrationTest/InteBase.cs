﻿//When integration testing using "on device" button DEVELOPMENT_BUILD is automatically on.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.TestTools;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

//This is now based on Unity 5.6's test runner. Separate Integration scene no longer required.
public abstract class InteBase {

    /// <summary>
    /// Do this before doing a scene load.
    /// </summary>
    protected static void ProtectTestRunner()
    {
        GameObject g = GameObject.Find("Code-based tests runner");
        GameObject.DontDestroyOnLoad(g);
    }

    /// <summary>
    /// Helper methods to save your pain
    /// </summary>
    /// <param name="seconds"></param>
    /// <returns></returns>
    protected static WaitForSeconds Wait(float seconds)
    {
        return new WaitForSeconds(seconds);
    }

    /// <summary>
    /// Unfortunately could not return T upon found, but useful for waiting something to become active
    /// </summary>
    /// <returns></returns>
    protected static IEnumerator WaitUntilFound<T>() where T : MonoBehaviour
    {
        T t = null;
        while (t == null)
        {
            t = (T)UnityEngine.Object.FindObjectOfType(typeof(T));
            yield return new WaitForSeconds(0.1f);
        }
    }

    protected static IEnumerator WaitUntilSceneLoaded(string sceneName)
    {
        while (IsSceneLoaded(sceneName) == false)
        {
            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// REMEMBER!! must be active..
    /// And remember that if there are multiples it returns the first one
    /// </summary>
    /// <returns></returns>
    protected static T Find<T>() where T : MonoBehaviour
    {
        return UnityEngine.Object.FindObjectOfType<T>() as T;
    }

    protected static T Find<T>(string sceneName) where T : MonoBehaviour
    {
        T[] objs = UnityEngine.Object.FindObjectsOfType<T>() as T[];
        foreach(T t in objs)
        {
            if(t.gameObject.scene.name == sceneName)
            {
                return t;
            }
        }
        return null;
    }

    /// <summary>
    /// This overload allows you to specify 2 types. It will try to find a child under that type with a given name.
    /// Useful for drilling down a prefab.
    /// </summary>
    protected static ChildType Find<ParentType, ChildType>(string childName, string sceneName = "") where ParentType : MonoBehaviour where ChildType : MonoBehaviour
    {
        ParentType find;
        if(sceneName == "")
        {
            find = Find<ParentType>();
        }
        else
        {
            find = Find<ParentType>(sceneName);
        }
        return FindChildRecursive(find.gameObject.transform, childName)?.GetComponent<ChildType>();
    }

    private static Transform FindChildRecursive(Transform transform, string childName)
    {
        Transform t = transform.Find(childName);
        if (t != null)
        {
            return t;
        }
        foreach (Transform child in transform)
        {
            Transform t2 = FindChildRecursive(child, childName);
            if (t2 != null)
            {
                return t2;
            }
        }
        return null;
    }
    
    //Get specific object name's component
    protected static T FindNamed<T>(string gameObjectName) where T : MonoBehaviour
    {
        GameObject go = GameObject.Find(gameObjectName);
        if (go != null)
        {
            return go.GetComponent<T>();
        }
        else
        {
            return null;
        }
    }


    /// <summary>
    /// Useful in case there are many T in the scene, usually from a separate sub-scene
    /// </summary>
    /// <param name="sceneName"></param>
    /// <returns></returns>
    protected static T FindOnSceneRoot<T>(string sceneName = "") where T : MonoBehaviour
    {
        Scene scene;
        if (sceneName == "")
        {
            scene = SceneManager.GetActiveScene();
        }
        else
        {
            scene = SceneManager.GetSceneByName(sceneName);
        }
        if (scene.IsValid() == true)
        {
            GameObject[] gos = scene.GetRootGameObjects();
            foreach (GameObject go in gos)
            {
                T component = go.GetComponent<T>();
                if (component != null)
                {
                    return component;
                }
            }
        }
        else
        {
            return null;
        }
        return null;
    }

    /// <summary>
    /// REMEMBER!! must be active..
    /// </summary>
    /// <returns></returns>
    protected static GameObject FindGameObject<T>() where T : MonoBehaviour
    {
        return (UnityEngine.Object.FindObjectOfType(typeof(T)) as T).gameObject;
    }

    protected static bool CheckGameObject(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// Time to utilize hacky way of using string...
    /// </summary>
    /// <param name="gameObjectName"></param>
    /// <returns></returns>
    public static Vector2 CenterOfRectNamed(string gameObjectName)
    {
        Vector3[] corners = new Vector3[4];
        GameObject go = GameObject.Find(gameObjectName);
        if (go != null)
        {
            go.GetComponent<RectTransform>().GetWorldCorners(corners);
            return Vector3.Lerp(Vector3.Lerp(corners[0], corners[1], 0.5f), Vector3.Lerp(corners[2], corners[3], 0.5f), 0.5f);
        }
        else
        {
            Debug.LogError("Can't find " + gameObjectName);
            return Vector2.zero;
        }
    }

    public static Vector2 CenterOfSpriteName(string gameObjectName)
    {
        GameObject go = GameObject.Find(gameObjectName);
        if (go != null)
        {
            return go.GetComponent<SpriteRenderer>().transform.position;
        }
        else
        {
            Debug.LogError("Can't find " + gameObjectName);
            return Vector2.zero;
        }
    }

    protected static bool IsSceneLoaded(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        return scene.IsValid() && scene.isLoaded;
    }

    /// <summary>
    /// Test with this attribute runs only in Unity editor
    /// </summary>
    public class UnityEditorPlatformAttribute : UnityPlatformAttribute
    {
        public UnityEditorPlatformAttribute()
        {
            this.include = new RuntimePlatform[] { RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor };
        }
    }

    /// <summary>
    /// Test with this attribute runs only on the real mobile device
    /// </summary>
    public class UnityMobilePlatformAttribute : UnityPlatformAttribute
    {
        public UnityMobilePlatformAttribute()
        {
            this.include = new RuntimePlatform[] { RuntimePlatform.Android, RuntimePlatform.IPhonePlayer };
        }
    }

    public void ActionBetweenSceneAwakeAndStart(string sceneName, System.Action action)
    {
        UnityEngine.Events.UnityAction<Scene,LoadSceneMode> unityAction = (scene,LoadSceneMode) =>
        {
            if(scene.name == sceneName)
            {
                action();
            }
        };

        SceneManager.sceneLoaded += unityAction;
    }

}

public static class UGUITestExtension 
{
    private static bool IsOutOfScreen(this Graphic graphic)
    {
        RectTransform rect = graphic.rectTransform;
        Vector3[] worldCorners = new Vector3[4];
        rect.GetWorldCorners(worldCorners); //This is already screen space not world! wtf!

        Camera activeCamera = Camera.main;
        Vector3 bottomLeft = worldCorners[0];
        Vector3 topLeft = worldCorners[1];
        //Vector3 topRight = worldCorners[2];
        Vector3 bottomRight = worldCorners[3];

        if(bottomLeft.x < Screen.width && bottomRight.x > 0 && topLeft.y > 0 && bottomRight.y < Screen.height)
        {
            return false; //Rect overlaps, therefore it is not out of screen
        }
        else
        {
            return true;
        }
    }
    
    private static bool HasZeroRectSize(this Graphic graphic) => graphic.rectTransform.rect.width == 0 || graphic.rectTransform.rect.height == 0;
    private static bool HasZeroScale(this Graphic graphic) => graphic.transform.localScale.x == 0 || graphic.transform.localScale.y == 0;

    /// <summary>
    /// An extension method to check visually can we see the graphic or not.
    /// It does not check for null Sprite since that will be rendered as a white rectangle.
    /// It does not check for transparency resulting from parent CanvasGroup.
    /// For Text, it does not account for empty text or truncated text.
    /// </summary>
    public static bool GraphicVisible(this Graphic graphic)
    {
        if (graphic.IsOutOfScreen() || graphic.HasZeroRectSize() || graphic.HasZeroScale() || graphic.gameObject.activeInHierarchy == false || graphic.enabled == false || graphic.color.a == 0 || ComponentInvisible(graphic))
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// In here we examine all other factors not common in Graphic if it makes the thing invisible or not.
    /// </summary>
    private static bool ComponentInvisible(Graphic graphic)
    {
        Text t  = graphic.GetComponent<Text>();
        if(t != null && t.text == "")
        {
            return true;
        }

        return false; //It's visible
    }
}


#endif