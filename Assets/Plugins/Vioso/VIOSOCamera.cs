using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using Unity.VisualScripting;
using System.Security.Cryptography;

public class VIOSOCamera : MonoBehaviour
{
    private int iPrinted,iPrinted2 = 0;
    public Text logProperties;
    public Text logType;
    public Text logIndex;
    public Material FOVmat;
    bool bDrawnFrustum=false;
    bool bActiveCamera=false;
    int frustumID=0;


    public enum ERROR
    {
        NONE = 0,         /// No error, we succeeded
        GENERIC = -1,     /// a generic error, this might be anything, check log file
        PARAMETER = -2,   /// a parameter error, provided parameter are missing or inappropriate
        INI_LOAD = -3,    /// ini could notbe loaded
        BLEND = -4,       /// blend invalid or coud not be loaded to graphic hardware, check log file
        WARP = -5,        /// warp invalid or could not be loaded to graphic hardware, check log file
        SHADER = -6,      /// shader program failed to load, usually because of not supported hardware, check log file
        VWF_LOAD = -7,    /// mappings file broken or version mismatch
        VWF_FILE_NOT_FOUND = -8, /// cannot find mapping file
        NOT_IMPLEMENTED = -9,     /// Not implemented, this function is yet to come
        NETWORK = -10,        /// Network could not be initialized
        FALSE = -16,        /// No error, but nothing has been done
    };

    //private const string dllLoc = "GfxPlugin_VIOSO64";// @"D:\Unity\1st\Assets\Plugins\GfxPlugin_VIOSO64.dll";
    [DllImport("VIOSO_Plugin64")]
    private static extern IntPtr GetRenderEventFunc();
    [DllImport("VIOSO_Plugin64")]
    private static extern ERROR Init(ref int id, string name, string pathToIni = "");
    [DllImport("VIOSO_Plugin64")]
    private static extern ERROR UpdateTex(int id, IntPtr texHandleSrc, IntPtr texHandleDest );
    [DllImport("VIOSO_Plugin64")]
    private static extern ERROR Destroy(int id);
    [DllImport("VIOSO_Plugin64")]
    private static extern ERROR GetError(int id, ref int err);
    [DllImport("VIOSO_Plugin64")]
    private static extern ERROR GetViewProj(int id, ref Vector3 pos, ref Vector3 rot, ref Matrix4x4 view, ref Matrix4x4 proj);
    [DllImport("VIOSO_Plugin64")]
    private static extern ERROR GetViewClip(int id, ref Vector3 pos, ref Vector3 rot, ref Matrix4x4 view, ref FrustumPlanes clip);
    [DllImport("VIOSO_Plugin64")]
    private static extern void SetTimeFromUnity(float t);
    [DllImport("VIOSO_Plugin64")]
    private static extern ERROR Is3D(int id, ref int b3D);

    private Camera cam;
    private ERROR err=ERROR.FALSE;
    private int viosoID = -1;
    private int b3D = 1;
    private Quaternion orig_rot = Quaternion.identity;
    private Vector3 orig_pos = Vector3.zero;
    private Dictionary<RenderTexture, IntPtr> texMap = new Dictionary<RenderTexture, IntPtr>();


    void Start()
    {

        //init UI
        logProperties.text= "";
        logIndex.text= "";
        logType.text= "";
        cam = GetComponent<Camera>();

        orig_rot = cam.transform.localRotation;
        orig_pos = cam.transform.localPosition;

        err = ERROR.FALSE;
        err = Init(ref viosoID, cam.name );
        if (ERROR.NONE == err)
        {
            GL.IssuePluginEvent(GetRenderEventFunc(), viosoID); // this will initialize warper in Unity Graphic Library context
            int err1 = 0;
            GetError(viosoID, ref err1);
            err = (ERROR)err1;
            if (ERROR.NONE != err)
            {
                Debug.Log("Initialization of warper failed.");
            }
        }
        else
        {
            Debug.Log(string.Format("Initialization attempt of warper failed with eror %i.", err ) );
        }

        if (ERROR.NONE != err)
        {
            Debug.Log("Failed to init camera.");
        }
    }

    private void OnDisable()
    {
        if (-1 != viosoID)
        {
            ERROR err = Destroy(viosoID);
            viosoID = -1;
        }
    }

    private void OnPreCull()
    {
        if (-1 != viosoID && ERROR.NONE == err)
        {
            ERROR err3D = Is3D(viosoID, ref b3D);
            //overwrite the views only when 3D data is detected
            if (1 == b3D && ERROR.NONE == err3D)
            {
                bActiveCamera = true;
                Vector3 pos = new Vector3(0, 0, 0);
                Vector3 rot = new Vector3(0, 0, 0);
                Matrix4x4 mV = Matrix4x4.identity;

                Matrix4x4 mP = new Matrix4x4();
                FrustumPlanes pl = new FrustumPlanes();
                if (ERROR.NONE == GetViewClip(viosoID, ref pos, ref rot, ref mV, ref pl))
                {
                    mV = mV.transpose;
                    Quaternion q = mV.rotation;
                    Vector3 p = mV.GetColumn(3);
                    cam.transform.localRotation = orig_rot * q;
                    cam.transform.localPosition = orig_pos + p;

                    mP = Matrix4x4.Frustum(pl);
                    cam.projectionMatrix = mP;
                    //UI: write the log after skipping 3 frames to get the final dir,fov
                    iPrinted++;
                    if (iPrinted < 4 && iPrinted > 2)
                    {
                        logIndex.text += (cam.name[cam.name.Length-1] +"\n" );
                        logType.text += ("3D \n");
                        float vfov = Mathf.Atan(1f / cam.projectionMatrix[1, 1]) * 2 * Mathf.Rad2Deg;
                        logProperties.text += ("Dir (x,y,z)° = " + cam.transform.localRotation.eulerAngles+" - VFOV="+vfov+"°"+"\n" );
                        
                        //Debug.Log("\n"+cam.name + " Pos: " +  cam.transform.localPosition + " Dir: " + cam.transform.localRotation.eulerAngles + " FOV: V" + cam.fieldOfView);
                        
                    }

                }
            }

            if (0 == b3D && ERROR.NONE == err3D && iPrinted2<4)
            {
                iPrinted2++;
                //UI: write the log after skipping 3 frames to get the final b3D
                if (iPrinted2 < 4 && iPrinted2 > 2) 
                {
                    logIndex.text += (cam.name[cam.name.Length - 1] + "\n");
                    logType.text += ("2D \n");
                    logProperties.text = ("Single perspective in wallpaper mode\n");
                } 
            }
        }  
        
    }


    private void OnRenderImage( RenderTexture source, RenderTexture destination )
    {
        RenderTexture.active = destination;
        if (-1 != viosoID)
        {
            IntPtr dst;
            if (!texMap.TryGetValue(source, out dst))
            {
                dst = source.GetNativeTexturePtr();
                texMap[source] = dst;
            }
            UpdateTex(viosoID, dst, IntPtr.Zero);
            SetTimeFromUnity(Time.timeSinceLevelLoad);
            GL.IssuePluginEvent(GetRenderEventFunc(), viosoID);
        }
    }

    /// <summary>
    /// called by checkbox: drawfrustums
    /// </summary>
    public void DrawFrustum()
    {
        if (!bDrawnFrustum && bActiveCamera)
        {
            bDrawnFrustum = true;
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube); //makes a cube
            frustumID = g.GetInstanceID();//get object ID to destroy it later
            Destroy(g.GetComponent<BoxCollider>()); //destroy the box collider on the cube because it's not needed
            MeshFilter meshFilter = g.GetComponent<MeshFilter>(); //get the meshfilter on cube
            g.GetComponent<Renderer>().material = FOVmat;
            Mesh mesh = new Mesh();
            Vector3[] points = new Vector3[5];
            points[0] = cam.transform.position;
            points[1] = cam.ViewportToWorldPoint(new Vector3(0, 0, 2));
            points[2] = cam.ViewportToWorldPoint(new Vector3(0, 1, 2));
            points[3] = cam.ViewportToWorldPoint(new Vector3(1, 0, 2));
            points[4] = cam.ViewportToWorldPoint(new Vector3(1, 1, 2));
            mesh.vertices = new Vector3[] {
             points[0], points[1], points[2],
             points[0], points[3], points[1],
             points[0], points[4], points[2],
             points[0], points[3], points[4],
             points[1], points[2], points[4],
             points[1], points[4], points[3] };

            mesh.triangles = new int[] {
                0, 1, 2,
                3, 4, 5,
                8, 7, 6,
                11, 10, 9,
                14, 13, 12,
                17, 16, 15};

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.MarkDynamic();
            //set the new mesh to cube's mesh
            meshFilter.mesh = mesh;
            //set the camera as the cube's parent
            g.transform.SetParent(cam.transform);
        }
        else if (bActiveCamera)
        {  
            GameObject.Destroy(transform.GetChild(0).gameObject);
            bDrawnFrustum = false;
        }
    }
    

}
