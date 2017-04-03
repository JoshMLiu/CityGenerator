using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class LoadMap : MonoBehaviour {

	public static float scale = 10000f;
	public static int subdivisionsx = 50;
	public static int landiterations = 0;
	public int subdivisionsy;
	public float landpercentage = 0.5f;
	public float waterpercentage = 0.5f;

	float minlat, minlon, maxlat, maxlon;
	float width, height;

	Area[,] areas;
	int areasperstat = 10;
	int totallandcount;

	void Start () {

		XmlDocument doc = new XmlDocument ();
		doc.Load(Application.dataPath + "/Resources/Maps/NewYork"); 

		setBounds(doc.GetElementsByTagName ("bounds") [0].Attributes);

		float ratio = (float)height / (float)width;
		subdivisionsy = (int)Mathf.Ceil (ratio * ((float)subdivisionsx));
		areas = new Area[subdivisionsx, subdivisionsy];

		Dictionary<string, Node> nodelist = new Dictionary<string, Node> ();
		setNodes (nodelist, doc);

		XmlNodeList ways = doc.GetElementsByTagName ("way");

		//createGround ();
		initializeTerrain ();
		initializeWater ();

		divideNodes (nodelist, doc, ways);
		setAreas (nodelist, ways);

		// Debug
		//System.Console.WriteLine ("original map");
		//printAreas ();

		changeLand ();
		setCoasts ();
		setStatistics ();

		//renderAreas ();
		setTerrain ();

		subdivideAreas ();
		renderSubdivisions ();

	}
		
	void OnApplicationQuit() {

		deleteAll ();

	}

	void deleteAll() {

		GameObject[] objs = GameObject.FindGameObjectsWithTag ("Prefab");
		print (objs.Length);
		foreach (GameObject o in objs) {
			Destroy (o.gameObject);
		}
		Resources.UnloadUnusedAssets ();

	}


	public void generateRoads() {



	}

	public void setStatistics() {

		List<XmlNode> randomnodes = new List<XmlNode>();
		List<XmlNode> randomways = new List<XmlNode>();

		int totallcount = 0;
		for (int j = 0; j < subdivisionsy; j += areasperstat) {
			for (int i = 0; i < subdivisionsx; i += areasperstat) {

				Statistics stat = new Statistics ();
				int lcount = 0;

				for (int k = 0; k < areasperstat; k++) {
					for (int p = 0; p < areasperstat; p++) {
						if (i + k >= subdivisionsx || j + p >= subdivisionsy) {
							continue;
						}
						Area a = areas [i + k, j + p];
						if (a.type == Area.Type.LAND) {
							lcount++;
							totallcount++;
						}
					}
				}

				if (lcount > (areasperstat * areasperstat) / 3) {
					randomnodes = new List<XmlNode>();
					randomways = new List<XmlNode>();
				}
					
				for (int k = 0; k < areasperstat; k++) {
					for (int p = 0; p < areasperstat; p++) {
						if (i + k >= subdivisionsx || j + p >= subdivisionsy) {
							continue;
						}
						Area a = areas [i + k, j + p];
						if (a.type == Area.Type.LAND) {
							
							if (lcount <= (areasperstat * areasperstat) / 3) {
								if (randomnodes.Count != 0 && randomways.Count != 0) {
									a.regionnodes = randomnodes;
									a.regionwaynodes = randomways;
								}
							} else {

								float ratio = 1/ (float)lcount;
								foreach (XmlNode x in a.regionnodes) {
									float rand = Random.Range (0f, 1f);
									if (rand <= ratio) {
										randomnodes.Add (x);
									}
								}
								foreach (XmlNode w in a.regionwaynodes) {
									float rand = Random.Range (0f, 1f);
									if (rand <= ratio) {
										randomways.Add (w);
									}
								}

							}

							stat.addArea (a);

						}
					}
				}

				stat.compileData ();
				stat.landcount = lcount;

				System.Console.WriteLine ("------------------------");
				System.Console.WriteLine (i + " " + j);
				stat.printStats ();
				System.Console.WriteLine ("------------------------");

				for (int k = 0; k < areasperstat; k++) {
					for (int p = 0; p < areasperstat; p++) {
						if (i + k >= subdivisionsx || j + p >= subdivisionsy) {
							continue;
						}
						Area a = areas [i + k, j + p];
						a.statistics = stat;
					}
				}

			}
		}
		totallandcount = totallcount;

	}

	public void subdivideAreas() {
		for (int i = 0; i < subdivisionsx; i++) {
			for (int j = 0; j < subdivisionsy; j++) {
				Area a = areas [i, j];
				if (a.type != Area.Type.LAND) {
					continue;
				}
				a.setRandomSubdivisions();
			}
		}
	}

	public void renderSubdivisions() {

		int totalnodecount = 0;
		for (int i = 0; i < subdivisionsx; i++) {
			for (int j = 0; j < subdivisionsy; j++) {
				totalnodecount += areas [i, j].regionnodes.Count;
				totalnodecount += areas [i, j].regionwaynodes.Count;
			}
		}

		for (int i = 0; i < subdivisionsx; i++) {
			for (int j = 0; j < subdivisionsy; j++) {
				Area a = areas [i, j];
				if (a.type != Area.Type.LAND) {
					continue;
				}


				float avgnodesperarea = (((float) a.statistics.landcount / (float) totallandcount) * (float) totalnodecount)/(float) (areasperstat*areasperstat);
				float nodecount = (float) areas [i, j].regionnodes.Count;
				nodecount += (float)areas [i, j].regionwaynodes.Count;
				float outputchance = nodecount / avgnodesperarea;

				List<Subdivision> subs = a.subdivisions;
				foreach (Subdivision s in subs) {

					Mesh m = s.mesh;
					if (m == null) {
						continue;
					}

					Statistics.AreaType atype = s.stats.getRandomAreaType ();
						
					Material mat = Resources.Load ("Materials/Black") as Material;
					string name = "";

					if (atype == Statistics.AreaType.BUILDINGS) {

						mat = Resources.Load ("Materials/Black") as Material;
						name = "Buildings";

						List<Mesh> buildingmeshes = s.getBuildingMeshes (outputchance);

						foreach (Mesh bmesh in buildingmeshes) {

							GameObject b = Instantiate(Resources.Load ("Prefabs/Cube", typeof(GameObject)) as GameObject);
							b.transform.position = new Vector3 (0, 1.01f, 0);
							b.name = "inst";
							b.tag = "Prefab";

							MeshFilter bmf = b.GetComponent<MeshFilter>();
							bmf.mesh = bmesh;
							MeshRenderer bmr = b.GetComponent<MeshRenderer> ();
							bmr.sharedMaterial = mat;

						}

					}
					else if (atype == Statistics.AreaType.NATURAL) {

						mat = Resources.Load ("Materials/Green") as Material;
						name = "Natural";
					}
					else if (atype == Statistics.AreaType.OTHER) {

						Statistics.OtherType othertype = s.stats.getRandomOther ();

						if (othertype == Statistics.OtherType.PARKING) {
							mat = Resources.Load ("Materials/Grey") as Material;
							name = "Parking Lot";
						} else {
							mat = Resources.Load ("Materials/Blue") as Material;
							name = "Other";
						}
							
					}

					GameObject cube = Instantiate(Resources.Load ("Prefabs/Cube", typeof(GameObject)) as GameObject);
					cube.transform.position = new Vector3 (0, 1f, 0);
					cube.name = name;
					cube.tag = "Prefab";

					MeshFilter mf = cube.GetComponent<MeshFilter>();
					mf.mesh = m;
					MeshRenderer mr = cube.GetComponent<MeshRenderer> ();
					mr.sharedMaterial = mat;

				}
			}
		}
		Resources.UnloadUnusedAssets ();

	}

	public void setTerrain() {

		GameObject g = GameObject.Find ("Terrain");
		Terrain terrain = g.GetComponent<Terrain> ();
		TerrainData data = terrain.terrainData;

		float[,] heights = new float[data.heightmapWidth, data.heightmapHeight];

		for (int i = 0; i < data.heightmapWidth; i++) {
			for (int j = 0; j < data.heightmapHeight; j++) {

				float x = ((float)i / (float)terrain.terrainData.heightmapWidth) * (float)height;
				float y = ((float)j / (float)terrain.terrainData.heightmapHeight) * (float)width;

				int areax = (int)Mathf.Floor (((float)y / (float)width)*(float)subdivisionsx);
				int areay = (int)Mathf.Floor (((float)x / (float)height)*(float)subdivisionsy);

				if (areas [areax, areay].type == Area.Type.LAND) {
					float h = Mathf.PerlinNoise (x, y)/1.2f;
					heights [i, j] = 0.5f + h;
				} else if (areas [areax, areay].type == Area.Type.COAST) {
					if (areas [areax, areay].containsPointInMesh (new Vector2 (y, x))) {
						float h = Mathf.PerlinNoise (x, y)/1.2f;
						Vector2 offset = new Vector2 (areas [areax, areay].getHalfX (), areas [areax, areay].getHalfY ()) - new Vector2 (y, x);
						float dist = offset.magnitude;
						heights [i, j] = 0.5f + h - 1f/dist;
					} else {
						float h = Mathf.PerlinNoise (x, y)/1.2f;
						heights [i, j] = h - 0.45f;
					}
				}
				else {
					heights [i, j] = 0;
				}


			}
		}
		data.SetHeights (0, 0, heights);

	}
		
	public void initializeTerrain() {

		GameObject g = GameObject.Find ("Terrain");
		Terrain terrain = g.GetComponent<Terrain> ();
		TerrainData data = terrain.terrainData;
		data.size = new Vector3 (width, 1f, height);

		Material m = Resources.Load ("Materials/Green") as Material;
		terrain.materialTemplate = m;
	}

	public void initializeWater() {

		GameObject g = GameObject.Find ("WaterProDaytime");
		g.transform.position = new Vector3 (width / 2, 0.25f, height / 2);
		g.transform.localScale = new Vector3 (height, 1f, height);

	}

	public void setCoasts() {

		for (int i = 0; i < subdivisionsx; i++) {
			for (int j = 0; j < subdivisionsy; j++) {
				Area a = areas [i, j];
				if (a.type == Area.Type.COAST) {
					for (int h = -1; h <= 1; h++) { 
						for (int k = -1; k <= 1; k++) {
							if (i + h >= 0 && i + h < subdivisionsx && j + k >= 0 && j + k < subdivisionsy) {
								if (areas [i + h, j + k].type == Area.Type.LAND) {
									if (h == -1 && k == 0) {
										// left
										a.coastmap.setSide (CoastMap.Location.LEFT);
									} else if (h == 1 && k == 0) {
										// right 
										a.coastmap.setSide (CoastMap.Location.RIGHT);
									} else if (h == 0 && k == 1) {
										// top
										a.coastmap.setSide (CoastMap.Location.TOP);
									} else if (h == 0 && k == -1) {
										// bottom
										a.coastmap.setSide (CoastMap.Location.BOTTOM);
									} else if (h == -1 && k == 1) {
										// topleft
										a.coastmap.setSide (CoastMap.Location.TOPLEFT);
									} else if (h == 1 && k == 1) {
										// topright
										a.coastmap.setSide (CoastMap.Location.TOPRIGHT);
									} else if (h == -1 && k == -1) {
										// bottomleft
										a.coastmap.setSide (CoastMap.Location.BOTTOMLEFT);
									} else if (h == 1 && k == -1) {
										// bottomright
										a.coastmap.setSide (CoastMap.Location.BOTTOMRIGHT);
									} 
								}
							}
						}
					}
				}
				a.createMeshes ();
			}
		}

	}

	public void changeLand() {

		for (int t = 0; t < landiterations; t++) {

			char typeflag = 'C';
			for (int i = 0; i < subdivisionsx; i++) {
				for (int j = 0; j < subdivisionsy; j++) {
					if (areas [i, j].type == Area.Type.COAST) {
						float rand = Random.Range (0f, 1f);
						if (!(rand > landpercentage + waterpercentage)) {
							float innerrand = Random.Range (0f, 1f);
							float totalratio = landpercentage + waterpercentage;
							float landratio = landpercentage / totalratio;

							if (innerrand <= landratio) {
								areas [i, j].type = Area.Type.LAND;
								typeflag = 'L';
								for (int h = -1; h <= 1; h++) { 
									for (int k = -1; k <= 1; k++) {
										if (i + h >= 0 && i + h < subdivisionsx && j + k >= 0 && j + k < subdivisionsy) {
											if (areas [i + h, j + k].type == Area.Type.LAND) {
												areas[i, j].regionnodes = new List<XmlNode> ();
												foreach (XmlNode x in areas[i + h, j + k].regionnodes) {
													areas [i, j].regionnodes.Add (x);
												}
												areas [i, j].regionwaynodes = new List<XmlNode> ();
												foreach (XmlNode w in areas[i + h, j + k].regionwaynodes) {
													areas [i, j].regionwaynodes.Add (w);
												}
											}
										}
									}
								}
							}
							else {
								areas [i, j].type = Area.Type.WATER;
								areas [i, j].regionnodes = new List<XmlNode> ();
								areas [i, j].regionwaynodes = new List<XmlNode> ();
								typeflag = 'W';
							}
						} 
					}
				}
			}
				
			for (int i = 0; i < subdivisionsx; i++) {
				for (int j = 0; j < subdivisionsy; j++) {
					Area a = areas [i, j];
					if (a.type == Area.Type.LAND) {
						for (int h = -1; h <= 1; h++) { 
							for (int k = -1; k <= 1; k++) {
								if (i + h >= 0 && i + h < subdivisionsx && j + k >= 0 && j + k < subdivisionsy) {
									if (areas [i + h, j + k].type == Area.Type.WATER) {
										if (typeflag == 'L') {
											areas [i + h, j + k].type = Area.Type.COAST;
										} else if (typeflag == 'W') {
											a.type = Area.Type.COAST;
										}
									}
								}
							}
						}
					} 
				}
			}

			for (int i = 0; i < subdivisionsx; i++) {
				for (int j = 0; j < subdivisionsy; j++) {
					Area a = areas [i, j];
					if (a.type == Area.Type.COAST) {
						bool all = true;
						for (int h = -1; h <= 1; h++) { 
							for (int k = -1; k <= 1; k++) {
								if (i + h >= 0 && i + h < subdivisionsx && j + k >= 0 && j + k < subdivisionsy) {
									if (areas [i + h, j + k].type == Area.Type.LAND) {
										all = false;
									}
								}
							}
						}
						if (all) {
							a.type = Area.Type.WATER;
						}
					} 
				}
			}

			System.Console.WriteLine ("iteration: " + (t + 1));
			printAreas ();
		}

	}
		
	public void divideNodes(Dictionary<string, Node> nodelist, XmlDocument doc, XmlNodeList ways) {

		for (int i = 0; i < subdivisionsx; i++) {
			for (int j = 0; j < subdivisionsy; j++) {
				Area a = new Area (i * width / subdivisionsx, j * height / subdivisionsy, (i + 1) * width / subdivisionsx, (j + 1) * height / subdivisionsy);
				areas [i, j] = a;
			}
		}
			
		XmlNodeList nodes = doc.GetElementsByTagName ("node");
		foreach (XmlNode n in nodes) {
			float lon = float.Parse(n.Attributes [2].Value);
			float lat = float.Parse(n.Attributes [1].Value);
			Vector2 p = new Vector2 ((lon - minlon)*scale, (lat - minlat)*scale);
			bool done = false;
			for (int i = 0; i < subdivisionsx; i++) {
				done = false;
				for (int j = 0; j < subdivisionsy; j++) {
					if (areas [i, j].containsPoint (p)) {
						areas [i, j].regionnodes.Add (n);
						done = true;
						break;
					}
				}
				if (done)
					break;
			}
		}

		foreach (XmlNode w in ways) {

			if (!w.InnerXml.Contains ("addr:housenumber") && !w.InnerXml.Contains ("building:level")) {
				continue;
			}

			XmlNode noderef = w.SelectSingleNode ("nd");
			string refid = noderef.Attributes [0].Value;
			Node n = nodelist [refid];

			if (n == null) {
				continue;
			}

			float lon = n.Lon;
			float lat = n.Lat;
			Vector2 p = new Vector2 ((lon - minlon)*scale, (lat - minlat)*scale);
			bool done = false;
			for (int i = 0; i < subdivisionsx; i++) {
				done = false;
				for (int j = 0; j < subdivisionsy; j++) {
					if (areas [i, j].containsPoint (p)) {
						areas [i, j].regionwaynodes.Add (w);
						done = true;
						break;
					}
				}
				if (done)
					break;
			}
		}

	}

	public void setAreas(Dictionary<string, Node> nodelist, XmlNodeList ways) {

		foreach (XmlNode w in ways) {
			XmlNodeList children = w.ChildNodes;
			if (w.InnerXml.Contains ("riverbank") || w.InnerXml.Contains ("coastline")) {
				foreach (XmlNode child in children) {
					if (child.Name.Equals ("nd")) {
						string refid = child.Attributes [0].Value;
						Node n = null;
						nodelist.TryGetValue (refid, out n);
						if (n != null) {
							Vector2 p = new Vector2 ((n.Lon - minlon)*scale, (n.Lat - minlat)*scale);
							for (int i = 0; i < subdivisionsx; i++) {
								for (int j = 0; j < subdivisionsy; j++) {
									if (areas [i, j].containsPoint (p)) {
										areas [i, j].type = Area.Type.COAST;
									}
								}
							}
						}
					}
				}
			}
		}

		for (int i = 0; i < subdivisionsx; i++) {
			for (int j = 0; j < subdivisionsy; j++) {
				Area a = areas [i, j];
				List<XmlNode> xnodes = areas [i, j].regionnodes;
				if (a.type == Area.Type.WATER) {
					if (xnodes.Count >= 1) {
						a.type = Area.Type.LAND;
					}
				}
			}
		}

		for (int i = 0; i < subdivisionsx; i++) {
			for (int j = 0; j < subdivisionsy; j++) {
				Area a = areas [i, j];
				if (a.type == Area.Type.LAND) {
					for (int h = -1; h <= 1; h++) { 
						for (int k = -1; k <= 1; k++) {
							if (i + h >= 0 && i + h < subdivisionsx && j + k >= 0 && j + k < subdivisionsy) {
								if (areas [i + h, j + k].type == Area.Type.WATER) {
									a.type = Area.Type.COAST;
								}
							}
						}
					}
				} 
			}
		}

		for (int i = 0; i < subdivisionsx; i++) {
			for (int j = 0; j < subdivisionsy; j++) {
				Area a = areas [i, j];
				if (a.type == Area.Type.COAST) {
					bool all = true;
					for (int h = -1; h <= 1; h++) { 
						for (int k = -1; k <= 1; k++) {
							if (i + h >= 0 && i + h < subdivisionsx && j + k >= 0 && j + k < subdivisionsy) {
								if (areas [i + h, j + k].type == Area.Type.LAND) {
									all = false;
								}
							}
						}
					}
					if (all) {
						a.type = Area.Type.WATER;
					}
				} 
			}
		}

		// single water patches
		for (int i = 0; i < subdivisionsx; i++) {
			for (int j = 0; j < subdivisionsy; j++) {
				Area a = areas [i, j];
				if (a.type == Area.Type.WATER) {
					bool all = true;
					for (int h = -1; h <= 1; h++) { 
						for (int k = -1; k <= 1; k++) {
							if (i + h >= 0 && i + h < subdivisionsx && j + k >= 0 && j + k < subdivisionsy) {
								if (h == 0 && k == 0) {
									continue;
								}
								if (areas [i + h, j + k].type == Area.Type.WATER) {
									all = false;
								}
							}
						}
					}
					if (all) {
						for (int h = -1; h <= 1; h++) { 
							for (int k = -1; k <= 1; k++) {
								if (i + h >= 0 && i + h < subdivisionsx && j + k >= 0 && j + k < subdivisionsy) {
									areas [i + h, j + k].type = Area.Type.LAND;
								}
							}
						}
					}
				} 
			}
		}

		// coasts to land
		for (int i = 0; i < subdivisionsx; i++) {
			for (int j = 0; j < subdivisionsy; j++) {
				Area a = areas [i, j];
				if (a.type == Area.Type.COAST) {
					bool all = true;
					for (int h = -1; h <= 1; h++) { 
						for (int k = -1; k <= 1; k++) {
							if (i + h >= 0 && i + h < subdivisionsx && j + k >= 0 && j + k < subdivisionsy) {
								if (areas [i + h, j + k].type == Area.Type.WATER) {
									all = false;
								}
							}
						}
					}
					if (all) {
						a.type = Area.Type.LAND;
					}
				} 
			}
		}

	}

	public void setBounds(XmlAttributeCollection c) {
		foreach (XmlAttribute a in c) {
			if (a.Name.Equals ("minlat"))
				minlat = float.Parse (a.Value);
			else if (a.Name.Equals ("minlon"))
				minlon = float.Parse (a.Value);
			else if (a.Name.Equals ("maxlat"))
				maxlat = float.Parse (a.Value);
			else if (a.Name.Equals ("maxlon"))
				maxlon = float.Parse (a.Value);
		}
		height = (maxlat - minlat)*scale;
		width = (maxlon - minlon)*scale;
	}

	public void setNodes(Dictionary<string, Node> d, XmlDocument doc) {
		XmlNodeList nodes = doc.GetElementsByTagName ("node");
		foreach (XmlNode n in nodes) {
			XmlAttributeCollection col = n.Attributes;
			string id = "";
			float lat = 0f;
			float lon = 0f;
			foreach (XmlAttribute a in col) {
				if (a.Name.Equals ("id"))
					id = a.Value;
				else if (a.Name.Equals("lat"))
					lat = float.Parse(a.Value);
				else if (a.Name.Equals("lon"))
					lon = float.Parse(a.Value);	
			}
			d.Add(id, new Node(id, lat, lon));
		}
	}

	public void createGround() {
		GameObject cube = Instantiate(Resources.Load ("Prefabs/Cube", typeof(GameObject)) as GameObject);
		cube.transform.position = new Vector3 (width/2, -0.6f, height/2);
		cube.transform.localScale = new Vector3 (width, 1f, height);
		cube.transform.name = "Ground";
		cube.tag = "Prefab";
	}

	public Vector2 getMins() {
		return new Vector2 (minlat, minlon);
	}

	public Vector2 getMaxes() {
		return new Vector2 (maxlat, maxlon);
	}

	public void printAreas() {

		System.Console.WriteLine ("Rotated Map");
		for (int i = subdivisionsy - 1; i >= 0; i--) {
			string s = "";
			for (int j = 0; j < subdivisionsx; j++) {
				s += areas [j, i].typeChar() + " ";
			}
			System.Console.WriteLine (s);
		}

		System.Console.WriteLine ("Real Map");
		for (int i = 0; i < subdivisionsx; i++) {
			string s = "";
			for (int j = 0; j < subdivisionsy; j++) {
				s += areas [i, j].typeChar () + " ";
			}
			System.Console.WriteLine (s);
		}

	}
 
	public void renderAreas() {

		for (int i = 0; i < subdivisionsx; i++) {
			for (int j = 0; j < subdivisionsy; j++) {
				
				Area a = areas [i, j];

				if (a.type == Area.Type.LAND) {
					Material m = Resources.Load ("Materials/Green") as Material;

					GameObject cube = Instantiate(Resources.Load ("Prefabs/Cube", typeof(GameObject)) as GameObject);
					cube.transform.position = new Vector3 (a.getHalfX (), 0f, a.getHalfY ());
					cube.transform.localScale = new Vector3 (a.getWidth (), 0.01f, a.getHeight ());
					cube.name = "Land";
					cube.tag = "Prefab";

					MeshRenderer mr = cube.GetComponent<MeshRenderer> ();
					mr.material = m;

				} else if (a.type == Area.Type.WATER) {
					
					Material m = Resources.Load ("Materials/Blue") as Material;

					GameObject cube = Instantiate(Resources.Load ("Prefabs/Cube", typeof(GameObject)) as GameObject);
					cube.transform.position = new Vector3 (a.getHalfX (), 0f, a.getHalfY ());
					cube.transform.localScale = new Vector3 (a.getWidth (), 0.01f, a.getHeight ());
					cube.name = "Water";
					cube.tag = "Prefab";

					MeshRenderer mr = cube.GetComponent<MeshRenderer> ();
					mr.material = m;

				} else if (a.type == Area.Type.COAST) {
					
					Material blue = Resources.Load ("Materials/Blue") as Material;

					GameObject waterbase = Instantiate(Resources.Load ("Prefabs/Cube", typeof(GameObject)) as GameObject);
					waterbase.transform.position = new Vector3 (a.getHalfX (), 0f, a.getHalfY ());
					waterbase.transform.localScale = new Vector3 (a.getWidth (), 0.01f, a.getHeight ());
					waterbase.name = "Water";
					waterbase.tag = "Prefab";

					MeshRenderer watermr = waterbase.GetComponent<MeshRenderer> ();
					watermr.material = blue;


					Material m = Resources.Load ("Materials/Green") as Material;

					List<Mesh> meshes = a.meshes;

					foreach (Mesh msh in meshes) {

						GameObject cube = Instantiate(Resources.Load ("Prefabs/Cube", typeof(GameObject)) as GameObject);
						cube.transform.position = new Vector3 (0, 0.01f, 0);
						cube.name = "Coast";
						cube.tag = "Prefab";

						MeshFilter mf = cube.GetComponent<MeshFilter>();
						mf.mesh = msh;
						MeshRenderer mr = cube.GetComponent<MeshRenderer> ();
						mr.material = m;

					}

				}
					
			}
		}
			
	}

}
	