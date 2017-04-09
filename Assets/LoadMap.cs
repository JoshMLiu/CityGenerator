using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

// Calculates and creates meshes for map in the Main Scene
public class LoadMap : MonoBehaviour {

	// Scale map data which is in latitudes/longitudes
	public static float scale = 10000f;
	// Number of areas to divide the map into on the x axis
	public static int subdivisionsx = 40;
	// Number of land changing iterations to do
	public static int landiterations = 0;
	// Calculated based on x to get square grid
	public int subdivisionsy;
	// Chance of a coast turning into land or into water
	public float landpercentage = 0.5f;
	public float waterpercentage = 0.5f;

	// Bounds of map
	float minlat, minlon, maxlat, maxlon;
	float width, height;

	Area[,] areas;
	// Number of areas to include in each statistical area
	int areasperstat = 8;
	int totallandcount;

	void Start () {

		// Take information parsed from the menu
		GameObject prev = GameObject.Find ("Go");
		Information info = (Information)prev.GetComponent ("Information");
		string mapname = "NewYork";
		if (info != null) {
			subdivisionsx = info.subdivisionsx;
			landiterations = info.landiterations;
			areasperstat = info.areasperstat;
			landpercentage = info.landpercentage;
			waterpercentage = info.waterpercentage;
			mapname = info.mapname;
		}
		DestroyImmediate (prev);

		// Load map
		TextAsset textAsset = (TextAsset) Resources.Load ("Maps/" + mapname);
		XmlDocument doc = new XmlDocument ();
		doc.LoadXml (textAsset.text);

		// Calculate bounds
		setBounds(doc.GetElementsByTagName ("bounds") [0].Attributes);

		// Number of rows is based on width of columns
		float ratio = (float)height / (float)width;
		subdivisionsy = (int)Mathf.Ceil (ratio * ((float)subdivisionsx));
		areas = new Area[subdivisionsx, subdivisionsy];

		Dictionary<string, Node> nodelist = new Dictionary<string, Node> ();
		setNodes (nodelist, doc);
		XmlNodeList ways = doc.GetElementsByTagName ("way");

		initializeTerrain ();
		initializeWater ();

		// Divide the nodes in the map into their respective areas
		divideNodes (nodelist, doc, ways);
		setAreas (nodelist, ways);

		// Set camera position and lock movement
		GameObject cam = GameObject.Find ("Main Camera");
		float elevation = (width / 2) / Mathf.Tan (31f); // half of camera field of view
		cam.transform.position = new Vector3 (width / 2, -elevation, height / 2);
		cam.transform.rotation = Quaternion.Euler (90, 0, 0);
		GhostFreeRoamCamera ghost = (GhostFreeRoamCamera)cam.GetComponent ("GhostFreeRoamCamera");
		ghost.allowMovement = false;
		ghost.allowRotation = false;

	}

	// Plan to follow in the game loop
	public enum ExecutionPath
	{
		CHANGELAND,
		COASTS,
		TERRAIN,
		SUBDIVISIONS,
		BRIDGES,
		DONE
	}

	ExecutionPath path = ExecutionPath.CHANGELAND;
	int changecounter = 0;
	float timecounter = 0;
	bool menu = false;

	void Update() {

		// Load menu
		if (menu) {
			if (unload != null && unload.isDone) {
				Application.LoadLevel ("Loading");
			} 
			return;
		}

		// M -> back to menu. Esc -> exit.
		if (Input.GetKey (KeyCode.M)) {
			deleteAll ();
			menu = true;
		}
		if (Input.GetKey (KeyCode.Escape)) {
			deleteAll ();
			Application.Quit ();
		}

		// path
		if (path == ExecutionPath.CHANGELAND) {
			// do 1 coast iteration every loop
			deleteAll ();
			if (changecounter > 0) {
				changeLand ();
			}
			renderAreas ();
			changecounter++;
			if (changecounter >= landiterations) {
				path = ExecutionPath.COASTS;
			}
		} else if (path == ExecutionPath.COASTS) {
			// wait 2 seconds
			if (timecounter <= 2) {
				timecounter += Time.deltaTime;
				return;
			}
			// calculate the coasts and render the areas
			timecounter = 0;
			setCoasts ();
			deleteAll ();
			renderAreas ();
			path = ExecutionPath.TERRAIN;
		}
		else if (path == ExecutionPath.TERRAIN) {
			// wait 2 seconds
			if (timecounter <= 2) {
				timecounter += Time.deltaTime;
				return;
			}
			// render terrain
			deleteAll ();
			setTerrain ();
			path = ExecutionPath.SUBDIVISIONS;
		} else if (path == ExecutionPath.SUBDIVISIONS) {
			// Calculate subdivisions + buildings and render
			setStatistics ();
			subdivideAreas ();
			renderSubdivisions ();
			path = ExecutionPath.BRIDGES;
		} else if (path == ExecutionPath.BRIDGES) {
			// Calculate bridges and render
			createBridges ();
			path = ExecutionPath.DONE;
			// unlock camera
			GameObject cam = GameObject.Find ("Main Camera");
			GhostFreeRoamCamera ghost = (GhostFreeRoamCamera)cam.GetComponent ("GhostFreeRoamCamera");
			ghost.allowMovement = true;
			ghost.allowRotation = true;
		} 

	}
		
	void OnApplicationQuit() {

		deleteAll ();

	}

	// Delete all temporary objects and unload resources 
	AsyncOperation unload;
	void deleteAll() {

		GameObject[] objs = GameObject.FindGameObjectsWithTag ("Prefab");
		foreach (GameObject o in objs) {
			Destroy (o.gameObject);
		}
		unload = Resources.UnloadUnusedAssets ();
	
	}

	// Calculate statistics for each area
	public void setStatistics() {

		// Lists of the nodes/ways from random areas
		List<List<XmlNode>> randomnodes = new List<List<XmlNode>>();
		List<List<XmlNode>> randomways = new List<List<XmlNode>>();

		// For each statistical area
		int totallcount = 0;
		for (int j = 0; j < subdivisionsy; j += areasperstat) {
			for (int i = 0; i < subdivisionsx; i += areasperstat) {

				Statistics stat = new Statistics ();
				int lcount = 0;

				// Calculate the land ratio of the area (for building render chance)
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

				// for all areas in the statistic
				if (lcount > (areasperstat * areasperstat) / 4) {
					randomnodes = new List<List<XmlNode>>();
					randomways = new List<List<XmlNode>>();
				}
				for (int k = 0; k < areasperstat; k++) {
					for (int p = 0; p < areasperstat; p++) {
						if (i + k >= subdivisionsx || j + p >= subdivisionsy) {
							continue;
						}
						Area a = areas [i + k, j + p];
						if (a.type == Area.Type.LAND) {

							// If the statistical area has few land areas, take random sample nodes from other stat areas
							if (lcount <= (areasperstat * areasperstat) / 4 && randomnodes.Count > 0 && randomways.Count > 0) {
								int randnindex = Random.Range (0, randomnodes.Count);
								int randwindex = Random.Range (0, randomways.Count);
								a.regionnodes = randomnodes[randnindex];
								a.regionwaynodes = randomways[randwindex];
							} else {
								// else add some nodes to the random list to be used next iteration
								randomnodes.Add (a.regionnodes);
								randomways.Add (a.regionwaynodes);
							}
							stat.addArea (a);
						}
					}
				}
					
				// Calculates probabilities
				stat.compileData ();
				stat.landcount = lcount;

				// Assign the statistic to each area it applies to
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

	public void createBridges() {

		// Check if each area belongs to a new landmass (save), or an already recorded one.
		List<List<Area>> landmasses = new List<List<Area>> ();
		for (int i = 0; i < subdivisionsx; i++) {
			for (int j = 0; j < subdivisionsy; j++) {
				if (areas [i, j].type != Area.Type.LAND) {
					continue;
				}
				bool skip = false;
				foreach (List<Area> l in landmasses) {
					if (l.Contains (areas [i, j])) {
						skip = true;
						break;
					}
				}
				if (skip) {
					continue;
				}
				List<Area> lands = new List<Area> ();
				getLandmass (i, j, lands);
				landmasses.Add (lands);
			}
		}

		// Randomize the landmass list order 
		List<List<Area>> shuffledlist = new List<List<Area>> ();
		while (landmasses.Count > 0) {
			int randindex = Random.Range (0, landmasses.Count);
			List<Area> temp = landmasses[randindex];
			landmasses.RemoveAt (randindex);
			shuffledlist.Add (temp);
		}
			
		// Until 1 landmass remaining: pick 2 landmasses, create a bridge between them, join them as 1 landmass, loop.
		while (shuffledlist.Count > 1) {

			List<Area> first = shuffledlist [0];
			List<Area> second = shuffledlist [1];
			shuffledlist.RemoveAt (0);
			shuffledlist.RemoveAt (0);

			// joined landmass
			List<Area> newlist = new List<Area> ();
			foreach (Area ar in first) {
				newlist.Add (ar);
			}
			foreach (Area ar in second) {
				newlist.Add (ar);
			}
			shuffledlist.Add (newlist);

			// Get minimum distance points between the two landmasses
			float mindist = float.MaxValue;
			Vector2 firstpos = new Vector2();
			Vector2 secondpos = new Vector2();
			foreach (Area a in first) {
				foreach (Area b in second) {
					Vector2 halfa = new Vector2 (a.getHalfX (), a.getHalfY ());
					Vector2 halfb = new Vector2 (b.getHalfX (), b.getHalfY ());

					float dist = (halfb - halfa).magnitude;
					if (dist < mindist) {
						mindist = dist;
						firstpos = halfa;
						secondpos = halfb;
					}
				}
			}

			// Bridge is too short
			if (mindist < width/8f) {
				continue;
			}

			Vector2 toppos;
			Vector2 botpos;

			// Get the higher and lower points on the map
			if (firstpos.y >= secondpos.y) {
				toppos = firstpos;
				botpos = secondpos;
			} else {
				toppos = secondpos;
				botpos = firstpos;
			}
				
			Material mat = Resources.Load ("Materials/Grey") as Material;
			//width of bridge
			float bwidth = areas [0, 0].getWidth ()/4;

			// render the bridge
			float o = (toppos.x - botpos.x) / 2;
			float h = mindist / 2;
			float rot = Mathf.Asin (o / h);
			float degrees = Mathf.Rad2Deg * rot;

			Vector2 half = new Vector2 ((toppos.x + botpos.x) / 2, (toppos.y + botpos.y) / 2);

			GameObject bridge = Instantiate(Resources.Load ("Prefabs/Cube", typeof(GameObject)) as GameObject);
			bridge.transform.position = new Vector3 (half.x, 0.69f, half.y);
			bridge.transform.localScale = new Vector3 (bwidth, 0.01f, mindist);
			bridge.transform.rotation = Quaternion.Euler (0, degrees, 0);
			bridge.name = "bridge";
			bridge.tag = "Prefab";

			MeshRenderer bmr = bridge.GetComponent<MeshRenderer> ();
			bmr.sharedMaterial = mat;

		}


	}
		
	// For an input land area, get all the areas that belong to the same landmass
	public void getLandmass(int i, int j, List<Area> list) {

		list.Add (areas [i, j]);
		for (int h = -1; h <= 1; h++) { 
			for (int k = -1; k <= 1; k++) {
				if (i + h >= 0 && i + h < subdivisionsx && j + k >= 0 && j + k < subdivisionsy) {
					if (areas [i + h, j + k].type == Area.Type.LAND) {
						if (!list.Contains(areas[i + h, j+ k])) {
							getLandmass (i + h, j + k, list);
						} 
					}
				}
			}
		}

	}

	// Sets random subdivsions for an area
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
				// non land areas do not have subdivisions
				if (a.type != Area.Type.LAND) {
					continue;
				}

				// calculate the building spawn chance for every building spot in the subdivision
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

					// Based on calculated probabilities
					Statistics.AreaType atype = s.stats.getRandomAreaType ();
						
					Material mat = Resources.Load ("Materials/LightGrey") as Material;
					string name = "";

					// Subdivision is too small for buildings
					if (s.isempty) {
						goto Skip;
					}

					if (atype == Statistics.AreaType.BUILDINGS) {

						mat = Resources.Load ("Materials/Pavement") as Material;
						name = "Buildings";

						// Get random building meshes and render them
						List<MeshPack> buildingmeshes = s.getBuildingMeshes (outputchance);

						foreach (MeshPack mp in buildingmeshes) {

							Mesh bmesh = mp.mesh;
							Material bmat = Resources.Load ("Materials/Black") as Material;

							if (mp.btype == Statistics.BuildingType.TOWER) {
								bmat = Resources.Load ("Materials/LightGrey") as Material;
							} else if (mp.btype == Statistics.BuildingType.HOUSE) {
								bmat = Resources.Load ("Materials/Grey") as Material;
							} else if (mp.btype == Statistics.BuildingType.FOOD) {
								bmat = Resources.Load ("Materials/Red") as Material;
							} else if (mp.btype == Statistics.BuildingType.SCHOOL) {
								bmat = Resources.Load ("Materials/Yellow") as Material;
							} else if (mp.btype == Statistics.BuildingType.SHOP) {
								bmat = Resources.Load ("Materials/LightBlue") as Material;
							} else if (mp.btype == Statistics.BuildingType.PLACEOFWORSHIP) {
								bmat = Resources.Load ("Materials/White") as Material;
							}

							GameObject b = Instantiate(Resources.Load ("Prefabs/Cube", typeof(GameObject)) as GameObject);
							b.transform.position = new Vector3 (0, 0.5f, 0);
							b.name = "inst";
							b.tag = "Prefab";

							MeshFilter bmf = b.GetComponent<MeshFilter>();
							bmf.mesh = bmesh;
							MeshRenderer bmr = b.GetComponent<MeshRenderer> ();
							bmr.sharedMaterial = bmat;

						}

					}
					else if (atype == Statistics.AreaType.NATURAL) {
						// green ground
						mat = Resources.Load ("Materials/Green") as Material;
						name = "Natural";
					}
					else if (atype == Statistics.AreaType.OTHER) {

						// Building that spans the entire subdivision area
						Statistics.OtherType othertype = s.stats.getRandomOther ();

						if (othertype == Statistics.OtherType.PARKING) {
							mat = Resources.Load ("Materials/AlmostBlack") as Material;
							name = "Parking Lot";
						} else {

							if (othertype == Statistics.OtherType.ATTRACTION) {
								mat = Resources.Load ("Materials/Blue") as Material;
							} else if (othertype == Statistics.OtherType.LEISURE) {
								mat = Resources.Load ("Materials/Orange") as Material;
							} else {
								mat = Resources.Load ("Materials/Black") as Material;
							}

							name = "Other";
							Mesh omesh = s.getOtherMesh (othertype);

							GameObject o = Instantiate(Resources.Load ("Prefabs/Cube", typeof(GameObject)) as GameObject);
							o.transform.position = new Vector3 (0, 0.4f, 0);
							o.name = "inst";
							o.tag = "Prefab";

							MeshFilter omf = o.GetComponent<MeshFilter>();
							omf.mesh = omesh;
							MeshRenderer omr = o.GetComponent<MeshRenderer> ();
							omr.sharedMaterial = mat;

						}
							
					}

					Skip:

					// render the ground 
					GameObject cube = Instantiate(Resources.Load ("Prefabs/Cube", typeof(GameObject)) as GameObject);
					cube.transform.position = new Vector3 (0, 0.701f, 0);
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

		// Height map
		float[,] heights = new float[data.heightmapWidth, data.heightmapHeight];

		// For each point in the heightmap
		for (int i = 0; i < data.heightmapWidth; i++) {
			for (int j = 0; j < data.heightmapHeight; j++) {

				// Terrain coordinates are backwards... (swap x and y)
				float x = ((float)i / (float)terrain.terrainData.heightmapWidth) * (float)height;
				float y = ((float)j / (float)terrain.terrainData.heightmapHeight) * (float)width;

				// get the area the point belongs to
				int areax = (int)Mathf.Floor (((float)y / (float)width)*(float)subdivisionsx);
				int areay = (int)Mathf.Floor (((float)x / (float)height)*(float)subdivisionsy);

				if (areas [areax, areay].type == Area.Type.LAND) {
					// land has constant height
					heights [i, j] = 0.7f;
				} else if (areas [areax, areay].type == Area.Type.COAST) {
					// point is on land
					if (areas [areax, areay].containsPointInMesh (new Vector2 (y, x))) {
						
						float h = Mathf.PerlinNoise (x, y)/1.4f;
						CoastMap cm = areas [areax, areay].coastmap;

						float totaldist = (new Vector2 (areas [areax, areay].getMinX (), areas [areax, areay].getMinY ())
						                  - new Vector2 (areas [areax, areay].getMaxX (), areas [areax, areay].getMaxY ())).magnitude;
						Vector2 oppoint = new Vector2 ();

						// Get a gradient for the coast height. Coast slopes down towards the water.
						if (cm.bottom && cm.left) {
							oppoint = new Vector2 (areas [areax, areay].getMaxX (), areas [areax, areay].getMaxY ());
						} else if (cm.bottom && cm.right) {
							oppoint = new Vector2 (areas [areax, areay].getMinX (), areas [areax, areay].getMaxY ());
						} else if (cm.top && cm.left) {
							oppoint = new Vector2 (areas [areax, areay].getMaxX (), areas [areax, areay].getMinY ());
						} else if (cm.top && cm.right) {
							oppoint = new Vector2 (areas [areax, areay].getMinX (), areas [areax, areay].getMinY ());
						} else if (cm.left || cm.top || cm.right || cm.bottom) {
							oppoint = new Vector2 (areas [areax, areay].getHalfX (), areas [areax, areay].getHalfY ());
							if (cm.left || cm.right) {
								totaldist = areas [areax, areay].getWidth () / 2;
							} else {
								totaldist = areas [areax, areay].getHeight () / 2;
							}
						} else if (cm.topleft || cm.topright || cm.bottomleft || cm.bottomright) {
							oppoint = new Vector2 (areas [areax, areay].getHalfX (), areas [areax, areay].getHalfY ());
							totaldist = totaldist / 2;
						} 

						float offset = (oppoint - new Vector2 (y, x)).magnitude;
						float dist = ((totaldist - offset)/totaldist);
						
						// Calculate a height with the gradient and random perlin noise
						float final = 0.7f + h - dist;
						if (final > 0.7f) {
							final = 0.7f;
						}
						heights [i, j] = final;

					} else {
						float h = Mathf.PerlinNoise (x, y)/1.3f;
						heights [i, j] = h - 0.45f;
					}
				}
				else {
					// water has low constant height
					heights [i, j] = 0;
				}


			}
		}
		data.SetHeights (0, 0, heights);

		// Color terrain based on area type (land = road, other = ground);
		float[,,] splatmapData = new float[data.alphamapWidth, data.alphamapHeight, data.alphamapLayers];
		for (int y = 0; y < data.alphamapHeight; y++)
		{
			for (int x = 0; x < data.alphamapWidth; x++)
			{

				float px = (float)x * (width / (float)data.alphamapWidth);
				float py = (float)y * (height / (float)data.alphamapHeight);

				int i = (int)Mathf.Floor ((float)py / (height / (float)subdivisionsx));
				int j = (int)Mathf.Floor ((float)px / (width / (float)subdivisionsy));

				if (areas [i, j].type == Area.Type.COAST) {
					splatmapData [x, y, 0] = 1f;
					splatmapData [x, y, 1] = 0;
				} else {
					splatmapData [x, y, 1] = 1f;
					splatmapData [x, y, 0] = 0;
				}
			}
		}
		data.SetAlphamaps (0, 0, splatmapData);
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

	// For every coast, determine the side where it connects to the land (1 side, 2 sides, corner. etc...)
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
		
	// 1 iteration of the random coast changing algorithm. Probabilities are specified by the user
	public void changeLand() {

		char typeflag = 'C';
		for (int i = 0; i < subdivisionsx; i++) {
			for (int j = 0; j < subdivisionsy; j++) {
				if (areas [i, j].type == Area.Type.COAST) {
					float rand = Random.Range (0f, 1f);
					// Don't change
					if (!(rand > landpercentage + waterpercentage)) {
						float innerrand = Random.Range (0f, 1f);
						float totalratio = landpercentage + waterpercentage;
						float landratio = landpercentage / totalratio;
						// Change coast to land
						if (innerrand <= landratio) {
							areas [i, j].type = Area.Type.LAND;
							typeflag = 'L';
							for (int h = -1; h <= 1; h++) { 
								for (int k = -1; k <= 1; k++) {
									if (i + h >= 0 && i + h < subdivisionsx && j + k >= 0 && j + k < subdivisionsy) {
										// Take nodes from all surrounding lands
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
						// Change coast to water
						else {
							// Dump all nodes from this area
							areas [i, j].type = Area.Type.WATER;
							areas [i, j].regionnodes = new List<XmlNode> ();
							areas [i, j].regionwaynodes = new List<XmlNode> ();
							typeflag = 'W';
						}
					} 
				}
			}
		}

		// Change surrounding areas to correct types. If coast changed to land -> surrounding waters change to coasts
		// If coast changed to water -> surrounding land turns to coasts
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

		// If a coast does not touch land, turn it into water
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

	}
		
	// Seperate all the nodes into their repective areas by location
	public void divideNodes(Dictionary<string, Node> nodelist, XmlDocument doc, XmlNodeList ways) {

		// Initialize areas
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
					// Check if the area contains the node
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

			// Only these two ways are wanted
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

	// Initialize the types of the areas based on the map data
	public void setAreas(Dictionary<string, Node> nodelist, XmlNodeList ways) {

		// Get coasts
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

		// If area is not a coast and has a sufficient amount of nodes, it is considered land
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

		// Put coasts between land and water
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

		// If coasts do not touch land, turn them to water
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

		// Eliminate single water patches (water surrounded by only coasts)
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

		// If coasts do not touch water, turn to land
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

	// Get bounds from xml file
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

	// Fill the node dictionary
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

	// Not used, creates ground plane
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
 
	// Renders a color-coded plane for each area
	public void renderAreas() {

		for (int i = 0; i < subdivisionsx; i++) {
			for (int j = 0; j < subdivisionsy; j++) {
				
				Area a = areas [i, j];

				if (a.type == Area.Type.LAND) {
					Material m = Resources.Load ("Materials/Green") as Material;

					GameObject cube = Instantiate(Resources.Load ("Prefabs/Cube", typeof(GameObject)) as GameObject);
					cube.transform.position = new Vector3 (a.getHalfX (), 5.01f, a.getHalfY ());
					cube.transform.localScale = new Vector3 (a.getWidth (), 0.01f, a.getHeight ());
					cube.name = "Land";
					cube.tag = "Prefab";

					MeshRenderer mr = cube.GetComponent<MeshRenderer> ();
					mr.material = m;

				} else if (a.type == Area.Type.WATER) {
					
					Material m = Resources.Load ("Materials/Blue") as Material;

					GameObject cube = Instantiate(Resources.Load ("Prefabs/Cube", typeof(GameObject)) as GameObject);
					cube.transform.position = new Vector3 (a.getHalfX (), 5f, a.getHalfY ());
					cube.transform.localScale = new Vector3 (a.getWidth (), 0.01f, a.getHeight ());
					cube.name = "Water";
					cube.tag = "Prefab";

					MeshRenderer mr = cube.GetComponent<MeshRenderer> ();
					mr.material = m;

				} else if (a.type == Area.Type.COAST) {
					
					Material blue = Resources.Load ("Materials/Blue") as Material;

					GameObject waterbase = Instantiate(Resources.Load ("Prefabs/Cube", typeof(GameObject)) as GameObject);
					waterbase.transform.position = new Vector3 (a.getHalfX (), 5f, a.getHalfY ());
					waterbase.transform.localScale = new Vector3 (a.getWidth (), 0.01f, a.getHeight ());
					waterbase.name = "Water";
					waterbase.tag = "Prefab";

					MeshRenderer watermr = waterbase.GetComponent<MeshRenderer> ();
					watermr.material = blue;


					Material m = Resources.Load ("Materials/Green") as Material;

					List<Mesh> meshes = a.meshes;

					foreach (Mesh msh in meshes) {

						GameObject cube = Instantiate(Resources.Load ("Prefabs/Cube", typeof(GameObject)) as GameObject);
						cube.transform.position = new Vector3 (0, 5.01f, 0);
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
	