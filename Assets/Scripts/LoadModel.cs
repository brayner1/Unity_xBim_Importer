using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Xbim.Common;
using Xbim.Common.Exceptions;
using Xbim.Common.Federation;
using Xbim.Common.Geometry;
using Xbim.Common.Metadata;
using Xbim.Common.Step21;
using Xbim.Common.XbimExtensions;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.UtilityResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.IO;
using Xbim.IO.Esent;
using Xbim.IO.Memory;
using Xbim.IO.Step21;
using Xbim.IO.Xml;
using Xbim.IO.Xml.BsConf;
using Xbim.Ifc;
using Xbim.Ifc.Extensions;
using Xbim.XbimExtensions;
using Xbim.Geometry;
using Xbim.ModelGeometry;
using Xbim.ModelGeometry.Converter;
using Xbim.ModelGeometry.Scene;

public class LoadModel : MonoBehaviour {

    public MeshContainer container;
    public Material sampleMat;
    private Dictionary<int, Mesh> geometryInstancesMap; 
	// Use this for initialization
	void Start () {
        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();
        geometryInstancesMap = new Dictionary<int, Mesh>();
        //StartCoroutine("Load");
        Load();
        
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    private void Load()
    {
        const string fileName = "Assets/ifc/Project1.xBIM";
        Debug.Log("initializing model loading");
        using (var model = IfcStore.Open(fileName, accessMode: Xbim.IO.Esent.XbimDBAccess.ReadWrite))
        {
            Debug.Log("model loaded");
            if (model.GeometryStore == null)
                Debug.Log("Geometry Store is null. Model has no geometry information");
            else
            {
                Debug.Log("Geometry Store is ok. Starting geometry conversion");
                using (var reader = model.GeometryStore.BeginRead())
                {
                    
                    //var shapeGeometries = reader.ShapeGeometries;
                    var shapeInstances = reader.ShapeInstances;
                    int shapeInstancesCount = 0;
                    foreach (XbimShapeInstance shape in shapeInstances)
                    {
                        XbimMatrix3D transformation = shape.Transformation;
                        Matrix4x4 transf = new Matrix4x4(new Vector4((float)transformation.M11, (float)transformation.M12, (float)transformation.M13, (float)transformation.M14),
                            new Vector4((float)transformation.M21, (float)transformation.M22, (float)transformation.M23, (float)transformation.M24),
                            new Vector4((float)transformation.M31, (float)transformation.M32, (float)transformation.M33, (float)transformation.M34),
                            new Vector4((float)transformation.OffsetX, (float)transformation.OffsetY, (float)transformation.OffsetZ, (float)transformation.M44));
                        if (!geometryInstancesMap.ContainsKey(shape.ShapeGeometryLabel)) {
                            XbimShapeGeometry geometry = reader.ShapeGeometry(shape.ShapeGeometryLabel);
                            if (geometry.ShapeData.Length <= 0)
                                continue;
                            Mesh mesh = new Mesh();

                            //List<Vector3> vertexList = new List<Vector3>();
                            List<int> triangleList = new List<int>();
                            var ms = new MemoryStream(((IXbimShapeGeometryData)geometry).ShapeData);
                            var br = new BinaryReader(ms);
                            var tr = br.ReadShapeTriangulation();
                            int size = tr.Vertices.Count;

                            Vector3[] vertices = Point3DList_to_Vec3Array(tr.Vertices);
                            //vertexList.AddRange(vertices);
                            Vector3[] normals = new Vector3[vertices.Length];
                            int[] normsCount = new int[vertices.Length];
                            int normalCount = 0;
                            int faceCount = 0;
                            IList<XbimFaceTriangulation> facesList = tr.Faces;             
                            //Debug.Log("shape " + shapeInstancesCount + " has " + facesList.Count + " faces");
                            foreach (XbimFaceTriangulation face in facesList)
                            {
                                IList<int> indices = face.Indices;
                                foreach (int index in indices)
                                {
                                    triangleList.Add(index);
                                }
                                //Debug.Log("face " + faceCount + " has " + indices.Count + " indices");
                                
                                int i = 0;
                                if (face.IsPlanar)
                                {
                                    //Debug.Log("face " + faceCount + " is planar ");
                                    XbimPackedNormal normal = face.Normals[0];
                                    foreach (int index in indices)
                                    {
                                        normals[index] += new Vector3((float)normal.Normal.X, (float)normal.Normal.Y, (float)normal.Normal.Z);
                                        normsCount[index]++;
                                        normalCount++;
                                    }
                                }
                                else
                                {
                                    foreach (XbimPackedNormal normal in face.Normals)
                                    {
                                        int index = face.Indices[i];
                                        normals[index] += new Vector3((float)normal.Normal.X, (float)normal.Normal.Y, (float)normal.Normal.Z);
                                        normsCount[index]++;
                                        normalCount++;
                                        i++;
                                    }
                                }
                                
                                faceCount++;
                            }
                            for (int i = 0; i < normals.Length; i++)
                            {
                                normals[i] = new Vector3(normals[i].x / (float)normsCount[i], normals[i].y / (float)normsCount[i], normals[i].z / (float)normsCount[i]).normalized;
                            }
                            //Debug.Log("shape " + shapeInstancesCount + " has " + size + " vertices");
                            //Debug.Log("shape " + shapeInstancesCount + " has " + XbimShapeTriangulation.TriangleCount(((IXbimShapeGeometryData)geometry).ShapeData) + " triangles");
                            //Debug.Log("shape " + shapeInstancesCount + " has " + normalCount + " normals");

                            mesh.vertices = vertices;
                            mesh.triangles = triangleList.ToArray();
                            mesh.normals = normals;
                            container.setMesh(mesh);
                            triangleList.Clear();
                            geometryInstancesMap.Add(shape.ShapeGeometryLabel, mesh);
                        } else
                        {
                            Mesh mesh;
                            geometryInstancesMap.TryGetValue(shape.ShapeGeometryLabel, out mesh);
                            if (mesh != null)
                                container.setMesh(mesh);
                        }


                        MeshContainer obj = Instantiate(container, new Vector3((float)transformation.OffsetX * 0.1f, (float)transformation.OffsetY * 0.1f, (float)transformation.OffsetZ * 0.1f), transf.rotation);
                        obj.transform.RotateAround(new Vector3(0, 0, 0), new Vector3(1, 0, 0), -90);
                        obj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                        var products = model.Instances.Where<IfcProduct>(e => e.EntityLabel == shape.IfcProductLabel);
                        int productCount = 0;
                        foreach(var product in products)
                        {
                            obj.name = product.Name.ToString();
                            //Material mat = obj.GetComponent<MeshRenderer>().material;
                            var isDefinedBy_inv = product.IsDefinedBy;
                            foreach(var rel_def in isDefinedBy_inv)
                            {
                                IfcPropertySetDefinitionSelect relating_property_def_select = rel_def.RelatingPropertyDefinition;
                                IfcPropertySetDefinition relating_property_def = (IfcPropertySetDefinition)relating_property_def_select;
                                if (relating_property_def != null)
                                {
                                    IfcPropertySet prop_set = (IfcPropertySet)relating_property_def;
                                    if (prop_set != null)
                                    {
                                        Material mat = readAppearanceFromPropertySet(prop_set);
                                        obj.GetComponent<Material>().color = mat.color;
                                            
                                    }
                                    continue;
                                }

                                IfcPropertySetDefinitionSet relating_property_def_set = (IfcPropertySetDefinitionSet)relating_property_def_select;
                                if(relating_property_def_set != null)
                                {
                                    var vec_property_def = relating_property_def_set.PropertySetDefinitions;
                                    foreach(IfcPropertySetDefinition property_def in vec_property_def)
                                    {
                                        if(property_def != null)
                                        {
                                            IfcPropertySet prop_set = (IfcPropertySet)property_def;
                                            if (prop_set != null)
                                            {
                                                Material mat = readAppearanceFromPropertySet(prop_set);
                                                obj.GetComponent<Material>().color = mat.color;
                                            }
                                        }
                                    }
                                        
                                }

                            }
                            productCount++;
                        }
                        //Debug.Log("there is " + productCount + " products with label " + shape.IfcProductLabel);
                            
                        shapeInstancesCount++;
                        //yield return null;
                    }
                    container.setMesh(null);
                }
                Debug.Log("Geometry loaded. Starting visualization.");
            } // Close Geometry Store

        } // Close File
    }

    Material readAppearanceFromPropertySet(IfcPropertySet prop_set)
    {
        Material mat = new Material(sampleMat);
        foreach(IfcProperty prop in prop_set.HasProperties)
        {
            if (prop == null) continue;
            IfcComplexProperty complex_prop = (IfcComplexProperty)prop;
            //IfcComplexProperty complex_prop = (IfcComplexProperty)prop;
            if(complex_prop != null)
            {
                if (complex_prop.UsageName == null) continue;
                if (complex_prop.UsageName.Value.ToString().Equals("Color"))
                {
                    Color diff_color = readIfcComplexPropertyColor(complex_prop);
                    mat.color = diff_color;
                }
            }
        }
        return mat;
    }

    Color readIfcComplexPropertyColor(IfcComplexProperty complex_prop)
    {
        Color cor = new Color(1, 1, 1, 1);
        var vec_hasProperties = complex_prop.HasProperties;
        if (vec_hasProperties.Count < 3) return cor;
        IfcPropertySingleValue[] values = new IfcPropertySingleValue[3];
        values[0] = (IfcPropertySingleValue)complex_prop.HasProperties[0];
        values[1] = (IfcPropertySingleValue)complex_prop.HasProperties[1];
        values[2] = (IfcPropertySingleValue)complex_prop.HasProperties[2];
        if (values[0] != null && values[1] != null && values[2] != null)
        {
            IfcValue v1 = values[0].NominalValue;
            IfcValue v2 = values[1].NominalValue;
            IfcValue v3 = values[2].NominalValue;
            if (v1 != null && v2 != null && v3 != null)
            {
                IfcInteger v1_int = (IfcInteger)v1;
                IfcInteger v2_int = (IfcInteger)v2;
                IfcInteger v3_int = (IfcInteger)v3;
                if (v1_int != null && v2_int != null && v3_int != null)
                {
                    float r = (float)v1_int / 255.0f;
                    float g = (float)v2_int / 255.0f;
                    float b = (float)v3_int / 255.0f;
                    if (r < 0.05f && g < 0.05f && b < 0.05f)
                    {
                        r = 0.1f;
                        g = 0.12f;
                        b = 0.15f;
                    }
                    cor.r = r;
                    cor.g = g;
                    cor.b = b;
                }

            }
        }
        return cor;
    }

    private Vector3[] Point3DList_to_Vec3Array(IList<XbimPoint3D> vertices)
    {
        Vector3[] vectors = new Vector3[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            vectors[i] = Point3DtoVector3(vertices[i]);
        }
        return vectors;
    }

    private Vector3 Point3DtoVector3(XbimPoint3D point)
    {
        return new Vector3((float)point.X, (float)point.Y, (float)point.Z);
    }
}