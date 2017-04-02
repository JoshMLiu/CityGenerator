using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class Statistics {

	public int totalnodes;
	public int landcount;

	public enum AreaTypes {
		BUILDINGS,
		NATURAL,
		OTHER
	}

	public float buildingratio;
	public float naturalratio;
	public float otherratio;

	public enum BuildingTypes {
		TOWER,
		HOUSE,
		FOOD,
		SHOP,
		PLACEOFWORSHIP,
		SCHOOL
	}

	public float towerratio;
	public float houseratio;
	public float foodratio;
	public float shopratio;
	public float worshipratio;
	public float schoolratio;

	Dictionary<string, int> buildings = new Dictionary<string, int>();

	public float meantowerheight;
	public float towervariance;

	Dictionary<int, int> towerheights = new Dictionary<int, int>();

	public enum NaturalTypes {
		PARK,
		WOOD,
		WATER
	}

	public float parkratio;
	public float woodratio;
	public float waterratio;

	Dictionary<string, int> naturals = new Dictionary<string, int>();

	public enum OtherTypes {
		PARKING,
		ATTRACTION,
		LEISURE,
		FUEL
	}

	public float parkingratio;
	public float attractionratio;
	public float leisureratio;
	public float fuelratio;

	Dictionary<string, int> others = new Dictionary<string, int>();

	public Statistics () {
	}

	public void compileData() {

		int buildingcount = 0;
		int naturalcount = 0;
		int othercount = 0;

		foreach (KeyValuePair<string, int> entry in buildings) {
			buildingcount += entry.Value;
		}

		foreach (KeyValuePair<string, int> entry in naturals) {
			naturalcount += entry.Value;
		}

		foreach (KeyValuePair<string, int> entry in others) {
			othercount += entry.Value;
		}

		totalnodes = buildingcount + naturalcount + othercount;

		buildingratio = (float)buildingcount / (float)totalnodes;
		naturalratio = (float)naturalcount / (float)totalnodes;
		otherratio = (float)othercount / (float)totalnodes; 

		towerratio = 0;
		houseratio = 0;
		foodratio = 0;
		shopratio = 0;
		worshipratio = 0;
		schoolratio = 0;

		foreach (KeyValuePair<string, int> entry in buildings) {
			if (entry.Key.Equals ("tower")) {
				towerratio = (float)entry.Value / (float)buildingcount;
			} else if (entry.Key.Equals ("house")) {
				houseratio = (float)entry.Value / (float)buildingcount;
			} else if (entry.Key.Equals ("food")) {
				foodratio = (float)entry.Value / (float)buildingcount;
			} else if (entry.Key.Equals ("shop")) {
				shopratio = (float)entry.Value / (float)buildingcount;
			} else if (entry.Key.Equals ("place_of_worship")) {
				worshipratio = (float)entry.Value / (float)buildingcount;
			} else if (entry.Key.Equals ("school")) {
				schoolratio = (float)entry.Value / (float)buildingcount;
			}
		}

		meantowerheight = 0;
		towervariance = 0;

		float towersum = 0;
		int numtowers = 0;
		foreach (KeyValuePair<int, int> entry in towerheights) {
			numtowers += entry.Value;
			towersum += (float)entry.Key * (float)entry.Value;
		}
		meantowerheight = towersum / numtowers;

		float squaresums = 0;
		foreach (KeyValuePair<int, int> entry in towerheights) {
			squaresums += Mathf.Pow((float)entry.Key - meantowerheight, 2) * (float)entry.Value;
		}
		towervariance = Mathf.Sqrt (squaresums / (float)(numtowers - 1));

		parkratio = 0;
		woodratio = 0;
		waterratio = 0;

		foreach (KeyValuePair<string, int> entry in naturals) {
			if (entry.Key.Equals ("park")) {
				parkratio = (float)entry.Value / (float)naturalcount;
			} else if (entry.Key.Equals ("wood")) {
				woodratio = (float)entry.Value / (float)naturalcount;
			} else if (entry.Key.Equals ("water")) {
				waterratio = (float)entry.Value / (float)naturalcount;
			} 
		}

		parkingratio = 0;
		attractionratio = 0;
		leisureratio = 0;
		fuelratio = 0;

		foreach (KeyValuePair<string, int> entry in others) {
			if (entry.Key.Equals ("parking")) {
				parkingratio = (float)entry.Value / (float)othercount;
			} else if (entry.Key.Equals ("attraction")) {
				attractionratio = (float)entry.Value / (float)othercount;
			} else if (entry.Key.Equals ("leisure")) {
				leisureratio = (float)entry.Value / (float)othercount;
			} else if (entry.Key.Equals ("fuel")) {
				fuelratio = (float)entry.Value / (float)othercount;
			} 
		}

			
	}

	public void addArea(Area a) {

		List<XmlNode> nodes = a.regionnodes;

		foreach (XmlNode x in nodes) {

			if (x.InnerText.Equals ("")) {
				continue;
			}

			XmlNodeList children = x.ChildNodes;
			foreach (XmlNode child in children) {

				System.Console.WriteLine ("k=" + k + " v=" + v);

				if (child.Name.Equals ("tag")) {

					string k = child.Attributes [0].Value;
					string v = child.Attributes [1].Value;

					if (k.Equals ("building:levels") && !v.Equals ("1")) {

						if (!buildings.ContainsKey ("tower")) {
							buildings.Add ("tower", 0);
						}
						int tval;
						buildings.TryGetValue ("tower", out tval);
						buildings.Add ("tower", tval + 1);
						int levels = int.Parse (v);
						if (!towerheights.ContainsKey (levels)) {
							towerheights.Add (levels, 0);
						}
						int thval;
						towerheights.TryGetValue (levels, out thval);
						towerheights.Add (levels, thval + 1);

					} else if (k.Equals ("amenity")) {

						if (v.Equals ("restaurant") || v.Equals ("fast_food")) {
							if (!buildings.ContainsKey ("food")) {
								buildings.Add ("food", 0);
							}
							int fval;
							buildings.TryGetValue ("food", out fval);
							buildings.Add ("food", fval + 1);
						} else if (v.Equals ("school")) {
							if (!buildings.ContainsKey ("school")) {
								buildings.Add ("school", 0);
							}
							int schval;
							buildings.TryGetValue ("school", out schval);
							buildings.Add ("school", schval + 1);
						} else if (v.Equals ("place_of_worship")) {
							if (!buildings.ContainsKey ("place_of_worship")) {
								buildings.Add ("place_of_worship", 0);
							}
							int pwval;
							buildings.TryGetValue ("place_of_worship", out pwval);
							buildings.Add ("place_of_worship", pwval + 1);
						} else if (v.Equals ("parking")) {
							if (!others.ContainsKey ("parking")) {
								others.Add ("parking", 0);
							}
							int pval;
							others.TryGetValue ("parking", out pval);
							others.Add ("parking", pval + 1);
						} else if (v.Equals ("fuel")) {
							if (!others.ContainsKey ("fuel")) {
								others.Add ("fuel", 0);
							}
							int fuelval;
							others.TryGetValue ("fuel", out fuelval);
							others.Add ("fuel", fuelval + 1);
						} 
							
					} else if (k.Equals ("shop")) {

						if (!buildings.ContainsKey ("shop")) {
							buildings.Add ("shop", 0);
						}
						int shpval;
						buildings.TryGetValue ("shop", out shpval);
						buildings.Add ("shop", shpval + 1);

					} else if (k.Equals ("building")) {

						if (v.Equals ("house") || v.Equals ("residential")) {
							if (!buildings.ContainsKey ("house")) {
								buildings.Add ("house", 0);
							}
							int hval;
							buildings.TryGetValue ("house", out hval);
							buildings.Add ("house", hval + 1);
						}

					} else if (k.Equals ("natural")) {

						if (v.Equals ("wood")) {
							if (!naturals.ContainsKey ("wood")) {
								naturals.Add ("wood", 0);
							}
							int wval;
							naturals.TryGetValue ("wood", out wval);
							naturals.Add ("wood", wval + 1);
						} else if (v.Equals ("water")) {
							if (!naturals.ContainsKey ("water")) {
								naturals.Add ("water", 0);
							}
							int watval;
							naturals.TryGetValue ("water", out watval);
							naturals.Add ("water", watval + 1);
						}

					} else if (k.Equals ("landuse")) {

						if (v.Equals ("forest")) {
							if (!naturals.ContainsKey ("wood")) {
								naturals.Add ("wood", 0);
							}
							int forval;
							naturals.TryGetValue ("wood", out forval);
							naturals.Add ("wood", forval + 1);
						}

					} else if (k.Equals ("leisure")) {

						if (v.Equals ("park")) {
							if (!naturals.ContainsKey ("park")) {
								naturals.Add ("park", 0);
							}
							int pkval;
							naturals.TryGetValue ("park", out pkval);
							naturals.Add ("park", pkval + 1);
						} else {
							if (!others.ContainsKey ("leisure")) {
								others.Add ("leisure", 0);
							}
							int lval;
							others.TryGetValue ("leisure", out lval);
							others.Add ("leisure", lval + 1);
						}

					} else if (k.Equals ("tourism")) {

						if (v.Equals ("attraction") || v.Equals ("museum")) {
							if (!others.ContainsKey ("attraction")) {
								others.Add ("attraction", 0);
							}
							int aval;
							others.TryGetValue ("attraction", out aval);
							others.Add ("attraction", aval + 1);
						}

					}

				} 
			}

		}

	}

	public void printStats() {

		System.Console.WriteLine ("Landcount = " + landcount);

		System.Console.WriteLine ("Totalcount = " + totalnodes);

		System.Console.WriteLine ("Building ratio = " + buildingratio);
		System.Console.WriteLine ("    -> Tower ratio = " + towerratio);
		System.Console.WriteLine ("        -> Avg Tower height = " + meantowerheight);
		System.Console.WriteLine ("        -> Tower variance = " + towervariance);
		System.Console.WriteLine ("    -> House ratio = " + houseratio);
		System.Console.WriteLine ("    -> Food ratio = " + foodratio);
		System.Console.WriteLine ("    -> Shop ratio = " + shopratio);
		System.Console.WriteLine ("    -> Place of Worship ratio = " + worshipratio);
		System.Console.WriteLine ("    -> School ratio = " + schoolratio);

		System.Console.WriteLine ("Natural ratio = " + naturalratio);
		System.Console.WriteLine ("    -> Park ratio = " + parkratio);
		System.Console.WriteLine ("    -> Wood ratio = " + woodratio);
		System.Console.WriteLine ("    -> Water ratio = " + waterratio);

		System.Console.WriteLine ("Other ratio = " + otherratio);
		System.Console.WriteLine ("    -> Parking ratio = " + parkingratio);
		System.Console.WriteLine ("    -> Attraction ratio = " + attractionratio);
		System.Console.WriteLine ("    -> Leisure ratio = " + leisureratio);
		System.Console.WriteLine ("    -> Fuel ratio = " + fuelratio);

	}
		
}
