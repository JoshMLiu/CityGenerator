using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class Statistics {

	public int totalnodes;
	public int landcount;

	public enum AreaType {
		BUILDINGS,
		NATURAL,
		OTHER
	}

	public float buildingratio;
	public float naturalratio;
	public float otherratio;

	public enum BuildingType {
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

	public enum NaturalType {
		PARK,
		WOOD,
		WATER
	}

	public float parkratio;
	public float woodratio;
	public float waterratio;

	Dictionary<string, int> naturals = new Dictionary<string, int>();

	public enum OtherType {
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

		int housecount = 0;
		buildings.TryGetValue ("house", out housecount);
		if (housecount > 0) {
			int newhousecount = (int) Mathf.Ceil ((float)housecount / 10);
			buildings ["house"] = newhousecount;
		}

		int parkcount = 0;
		naturals.TryGetValue ("park", out parkcount);
		if (parkcount > 0) {
			int newparkcount = (int) Mathf.Ceil ((float)parkcount*3);
			naturals ["park"] = newparkcount;
		}

		int woodcount = 0;
		naturals.TryGetValue ("wood", out woodcount);
		if (woodcount > 0) {
			int newwoodcount = (int) Mathf.Ceil ((float)woodcount*3);
			naturals ["wood"] = newwoodcount;
		}

		int foodcount = 0;
		buildings.TryGetValue ("food", out foodcount);
		if (foodcount > 0) {
			int newfoodcount = (int) Mathf.Ceil ((float)foodcount/2);
			buildings ["food"] = newfoodcount;
		}

		int shopcount = 0;
		buildings.TryGetValue ("shop", out shopcount);
		if (shopcount > 0) {
			int newshopcount = (int) Mathf.Ceil ((float)shopcount/2);
			buildings ["shop"] = newshopcount;
		}

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

		if (totalnodes > 0) {
			buildingratio = (float)buildingcount / (float)totalnodes;
			naturalratio = (float)naturalcount / (float)totalnodes;
			otherratio = (float)othercount / (float)totalnodes; 
		}

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

		if (numtowers > 0) {
			meantowerheight = towersum / numtowers;
			float squaresums = 0;
			foreach (KeyValuePair<int, int> entry in towerheights) {
				squaresums += Mathf.Pow((float)entry.Key - meantowerheight, 2) * (float)entry.Value;
			}
			if (numtowers == 1) {
				towervariance = meantowerheight;
			} else {
				towervariance = Mathf.Sqrt (squaresums / (float)(numtowers - 1));
			}
		} 

		if (towervariance == 0) {
			towervariance = meantowerheight;
		}
			
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

		List<XmlNode> ways = a.regionwaynodes;
		List<XmlNode> nodes = a.regionnodes;

		foreach (XmlNode w in ways) {

			if (w.InnerXml.Equals ("")) {
				continue;
			}
				
			foreach (XmlNode waychild in w.ChildNodes) {

				if (waychild.Name.Equals ("tag")) {

					string k = waychild.Attributes [0].Value;
					string v = waychild.Attributes [1].Value;

					if (k.Equals ("building:levels") && !v.Equals ("1")) {

						if (!buildings.ContainsKey ("tower")) {
							buildings.Add ("tower", 0);
						}
						int tval;
						buildings.TryGetValue ("tower", out tval);
						buildings["tower"] = tval + 1;
						int levels = 0;
						if (!int.TryParse (v, out levels)) {
							continue;
						}
						if (!towerheights.ContainsKey (levels)) {
							towerheights.Add (levels, 0);
						}
						int thval;
						towerheights.TryGetValue (levels, out thval);
						towerheights[levels] = thval + 1;

					} else if (k.Equals ("addr:housenumber")) {

						if (!buildings.ContainsKey ("house")) {
							buildings.Add ("house", 0);
						}
						int hval;
						buildings.TryGetValue ("house", out hval);
						buildings["house"] = hval + 1;

					} else if (k.Equals ("amenity")) {
						if (v.Equals("place_of_worship")) {
							if (!buildings.ContainsKey ("place_of_worship")) {
								buildings.Add ("place_of_worship", 0);
							}
							int pwval;
							buildings.TryGetValue ("place_of_worship", out pwval);
							buildings["place_of_worship"] = pwval + 1;
						}

					}

				}

			}
		}

		foreach (XmlNode x in nodes) {

			if (x.InnerXml.Equals ("")) {
				continue;
			}
				
			foreach (XmlNode child in x.ChildNodes) {

				if (child.Name.Equals ("tag")) {

					string k = child.Attributes [0].Value;
					string v = child.Attributes [1].Value;

					if (k.Equals ("amenity")) {

						if (v.Equals ("restaurant") || v.Equals ("fast_food")) {
							if (!buildings.ContainsKey ("food")) {
								buildings.Add ("food", 0);
							}
							int fval;
							buildings.TryGetValue ("food", out fval);
							buildings["food"] = fval + 1;
						} else if (v.Equals ("school")) {
							if (!buildings.ContainsKey ("school")) {
								buildings.Add ("school", 0);
							}
							int schval;
							buildings.TryGetValue ("school", out schval);
							buildings["school"] = schval + 1;
						} else if (v.Equals ("parking")) {
							if (!others.ContainsKey ("parking")) {
								others.Add ("parking", 0);
							}
							int pval;
							others.TryGetValue ("parking", out pval);
							others["parking"] = pval + 1;
						} else if (v.Equals ("fuel")) {
							if (!others.ContainsKey ("fuel")) {
								others.Add ("fuel", 0);
							}
							int fuelval;
							others.TryGetValue ("fuel", out fuelval);
							others["fuel"] = fuelval + 1;
						} 
							
					} else if (k.Equals ("shop")) {

						if (!buildings.ContainsKey ("shop")) {
							buildings.Add ("shop", 0);
						}
						int shpval;
						buildings.TryGetValue ("shop", out shpval);
						buildings["shop"] = shpval + 1;

					} else if (k.Equals ("natural")) {

						if (v.Equals ("wood")) {
							if (!naturals.ContainsKey ("wood")) {
								naturals.Add ("wood", 0);
							}
							int wval;
							naturals.TryGetValue ("wood", out wval);
							naturals["wood"] = wval + 1;
						} else if (v.Equals ("water")) {
							if (!naturals.ContainsKey ("water")) {
								naturals.Add ("water", 0);
							}
							int watval;
							naturals.TryGetValue ("water", out watval);
							naturals["water"] = watval + 1;
						}

					} else if (k.Equals ("landuse")) {

						if (v.Equals ("forest")) {
							if (!naturals.ContainsKey ("wood")) {
								naturals.Add ("wood", 0);
							}
							int forval;
							naturals.TryGetValue ("wood", out forval);
							naturals["wood"] = forval + 1;
						}

					} else if (k.Equals ("leisure")) {

						if (v.Equals ("park")) {
							if (!naturals.ContainsKey ("park")) {
								naturals.Add ("park", 0);
							}
							int pkval;
							naturals.TryGetValue ("park", out pkval);
							naturals["park"] = pkval + 1;
						} else {
							if (!others.ContainsKey ("leisure")) {
								others.Add ("leisure", 0);
							}
							int lval;
							others.TryGetValue ("leisure", out lval);
							others["leisure"] = lval + 1;
						}

					} else if (k.Equals ("tourism")) {

						if (v.Equals ("attraction")) {
							if (!others.ContainsKey ("attraction")) {
								others.Add ("attraction", 0);
							}
							int aval;
							others.TryGetValue ("attraction", out aval);
							others["attraction"] = aval + 1;
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

	public AreaType getRandomAreaType() {

		float rand = Random.Range (0f, 1f);

		if (rand <= buildingratio) {
			return AreaType.BUILDINGS;
		} else if (rand <= buildingratio + naturalratio) {
			return AreaType.NATURAL;
		} else {
			return AreaType.OTHER;
		}

	}

	public BuildingType getRandomBuilding() {

		float rand = Random.Range (0f, 1f);

		if (rand <= towerratio) {
			return BuildingType.TOWER;
		} else if (rand <= towerratio + houseratio) {
			return BuildingType.HOUSE;
		} else if (rand <= towerratio + houseratio + foodratio) {
			return BuildingType.FOOD;
		} else if (rand <= towerratio + houseratio + foodratio + shopratio) {
			return BuildingType.SHOP;
		} else if (rand <= towerratio + houseratio + foodratio + shopratio + worshipratio) {
			return BuildingType.PLACEOFWORSHIP;
		} else {
			return BuildingType.SCHOOL;
		}

	}

	public float getRandomTowerHeight() {

		float halfheight = towervariance / 2;
		float rand = Random.Range (-halfheight, halfheight);
		return meantowerheight + rand;

	}

	public NaturalType getRandomNatural() {

		float rand = Random.Range (0f, 1f);

		if (rand <= parkratio) {
			return NaturalType.PARK;
		} else if (rand <= parkratio + woodratio) {
			return NaturalType.WOOD;
		} else {
			return NaturalType.WATER;
		}
			
	}

	public OtherType getRandomOther() {

		float rand = Random.Range (0f, 1f);

		if (rand <= parkingratio) {
			return OtherType.PARKING;
		} else if (rand <= parkingratio + attractionratio) {
			return OtherType.ATTRACTION;
		} else if (rand <= parkingratio + attractionratio + leisureratio) {
			return OtherType.LEISURE;
		} else {
			return OtherType.FUEL;
		}
	
	}
		
}
