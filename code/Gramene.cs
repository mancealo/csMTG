using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csMTG
{
    public class Gramene : mtg
    {
        #region Attributes

        public Dictionary<int, string> labelsOfScales;

        int cursor = 0;
        int nbPlants = 0;
        int axisNumber = 0;
        int metamerNb = 0;

        //public List<double> laminaNStruct { get; set; }
        //public List<double> AxisNumberperLeafLayer { get; set; }
        //List<double> tillerNumberPerLeafLayer { get; set; }

        public Dictionary<string, List<double>> propertiesToAdd { get; set; }

        public Dictionary<string, List<double>> tillerResults { get; set; }//Key T_(tiller order)_(position wrt main axis)_(position wrt parent axis)
                                                                            //Value[0] number of leaves on main axis at axis creation
                                                                            //Value[1] number of leaves created on parent axis or main axis in case of T000
                                                                            //Value[2] Fraction of the axis
                                                                            //Value[3] Fraction of the last leaf on the Main Steme
        public double AverageShootNumberPerPlant { get; set; }
                                                                        


        #endregion

        #region Constructor and copy constructor

        /// <summary>
        /// Constructor of Gramene:
        /// * Sets the labels of the 6 scales of an mtg.
        /// </summary>
        public Gramene()
        {
            // define the 6 scales

            labelsOfScales = new Dictionary<int, string>();
            labelsOfScales.Add(1, "canopy");
            labelsOfScales.Add(2, "plant");
            labelsOfScales.Add(3, "system");
            labelsOfScales.Add(4, "axis");
            labelsOfScales.Add(5, "botanical_unit");
            labelsOfScales.Add(6, "organ");

        }


        /// <summary>
        /// Copy constructor of gramene.
        /// It copies the values of: scale, complex, components, parent, children, properties and the class attributes
        /// </summary>
        /// <param name="grameneToCopyFrom"> The gramene we want to copy from. </param>
        /// <param name="copyAll"></param>
        public Gramene(Gramene grameneToCopyFrom, bool copyAll = true)
        {
            // Copy of the scales
            foreach (int vertex in grameneToCopyFrom.scale.Keys)
            {
                if (vertex != 0)
                    this.scale.Add(vertex, (int)grameneToCopyFrom.Scale(vertex));
            }


            // Copy of the complexes
            foreach (int component in grameneToCopyFrom.complex.Keys)
                this.complex.Add(component, (int)grameneToCopyFrom.Complex(component));

            // Copy of the components
            foreach (int complex in grameneToCopyFrom.components.Keys)
                this.components.Add(complex, grameneToCopyFrom.Components(complex));

            // Copy of the parents
            foreach (int child in grameneToCopyFrom.parent.Keys)
            {
                if (child != 0)
                    this.parent.Add(child, (int)grameneToCopyFrom.Parent(child));
            }

            // Copy of the children
            foreach (int parent in grameneToCopyFrom.children.Keys)
                this.children.Add(parent, grameneToCopyFrom.Children(parent));

            // Copy of the properties
            foreach (string label in grameneToCopyFrom.PropertyNames())
            {
                if (this.properties.ContainsKey(label))
                    this.properties[label] = grameneToCopyFrom.Property(label);
                else
                    this.properties.Add(label, grameneToCopyFrom.Property(label));
            }

            cursor = grameneToCopyFrom.cursor;
            nbPlants = grameneToCopyFrom.nbPlants;

        }


        #endregion

        #region Cursor

        /// <summary>
        /// Gets the value of the cursor.
        /// </summary>
        /// <returns> Value of the cursor. </returns>
        public int GetCursor()
        {
            return cursor;
        }

        /// <summary>
        /// Updates the value of the cursor.
        /// </summary>
        /// <param name="vertexId"> Vertex identifier on which will be placed the cursor. </param>
        public void SetCursor(int vertexId)
        {
            cursor = vertexId;
        }

        /// <summary>
        /// Retrieve the identifier of the canopy based on the position of the cursor.
        /// The cursor is set to the last item visited. If it's in the right scale (1), it is equal to the canopy's id.
        /// Otherwise, we will iteratively look for the complex of the cursor until scale 1 is reached.
        /// </summary>
        /// <returns> The identifier of the canopy. </returns>
        int GetCanopyId()
        {
            int canopyCursor = cursor;

            while (Scale(canopyCursor) != 1 && canopyCursor != 0)
            {
                canopyCursor = (int)Complex(canopyCursor);
            }

            if (canopyCursor == 0)
                canopyCursor = AddCanopy();

            return canopyCursor;
        }

        /// <summary>
        /// Retrieve the actual plant's identifier.
        /// The cursor's scale should be equal to 2.
        /// In case the cursor's scale is lower than 2, the plant needs to be created first.
        /// In case the cursor's scale is greater than 2, we will iteratively look for the complex until scale 2 is reached.
        /// </summary>
        /// <returns> The identifier of the plant. </returns>
        int GetPlantId()
        {
            int plantId;

            if (Scale(cursor) == 2)
                plantId = cursor;
            else
            {
                if (Scale(cursor) < 2)
                    plantId = AddPlant();
                else
                {
                    plantId = ComplexAtScale(cursor, 2);

                }
            }

            return plantId;
        }

        /// <summary>
        /// Retrieve the actual shoot's identifier.
        /// The cursor's scale should be equal to 3.
        /// In case the cursor's scale is lower than 3, the shoot needs to be created first.
        /// In case the cursor's scale is greater than 2, we will iteratively look for the complex until scale 3 is reached.
        /// </summary>
        /// <returns> The identifier of the shoot. </returns>
        int GetShootId(Dictionary<string, List<double>> Tillers)
        {
            int shootId = cursor;

            if (Scale(shootId) == 3)
            {
                int plantId = (int)Complex(shootId);
                
                shootId = PlantHasShoot(plantId);

                if (shootId == 0)
                    AddShoot(Tillers);
            }
            else
            {
                if (Scale(shootId) < 3)
                    AddShoot(Tillers);
                else
                {
                    shootId = ComplexAtScale(shootId, 3);

                }
            }

            return shootId;

        }

        /// <summary>
        /// Retrieve the current axis' identifier.
        /// The cursor should be on the scale 4.
        /// If it's lower than that, we create a plant and shoot.
        /// If it's greater than that, we iteratively look for the complex at scale 4.
        /// </summary>
        /// <returns> Identifier of the axis. </returns>
        int GetAxisId(Dictionary<string, List<double>> Tillers)
        {
            int axisId = cursor;
            if (Scale(cursor) != 4)
            {
                if (Scale(cursor) < 4)
                    axisId = AddAxis(Tillers);
                else
                    axisId = ComplexAtScale(axisId, 4);
            }

            return axisId;
        }

        /// <summary>
        /// Retrieve the current metamer's identifier.
        /// The cursor should be on scale 5.
        /// If it's lower than that, we create a new metamer on the same axis.
        /// If it's greater than that, we iteratively look for the complex at scale 5.
        /// </summary>
        /// <returns> Identifier of the metamer. </returns>
        int GetMetamerId(int leafLayerIndexOfFirstLeaf,int leafIndexOntheParentAxis,Dictionary<string, List<double>> Tillers)
        {
            int metamerId = cursor;

            if (Scale(metamerId) != 5)
            {
                if (Scale(metamerId) > 5)
                    metamerId = ComplexAtScale(metamerId, 5);
                else
                    metamerId = AddMetamer(leafLayerIndexOfFirstLeaf, leafIndexOntheParentAxis, Tillers);
            }

            return metamerId;
        }

        #endregion

        #region Property utilities

        /// <summary>
        /// Calculate the property value to assign to a leaf vertex.
        /// Based on layer definiton = all phytomers (leaves) emerging at the same time
        /// </summary>
        ///<param name="propAtLeafLayerLevel">List containing values at the leaf layer level</param>
        ///<param name="leafLayerIndexOfFirstLeaf">Leaf Layer index of the first leaf of the axis</param>
        ///<param name="leafIndexOntheParentAxis">Leaf index on the parent axis</param>
        ///<param name="isAddedOnMainStem"> true, if the axes has to be added on mainstem 
        /// false, if added on a other axis. </param>
        /// <returns> The value to assign to leaf vertices. </returns>
        public double DistributeLeafLayerPropertyValues(List<double> propAtLeafLayerLevel, int leafLayerIndexOfFirstLeaf,int leafIndexOntheParentAxis,double leafFraction, bool isByPhytomer=true){

            double numberOfLeavesOnLayerPhytomer = 1.0;
            int LeafLayerIndex=0;

            LeafLayerIndex = leafLayerIndexOfFirstLeaf + leafIndexOntheParentAxis;

            numberOfLeavesOnLayerPhytomer = Math.Min((double)Fibonacci(LeafLayerIndex+1), Math.Ceiling(AverageShootNumberPerPlant));

            if (numberOfLeavesOnLayerPhytomer != 0 && isByPhytomer==true && leafFraction==1.0) return propAtLeafLayerLevel[LeafLayerIndex] / numberOfLeavesOnLayerPhytomer;
            else return propAtLeafLayerLevel[LeafLayerIndex];


            

        }

        private int Fibonacci(int N)
        {
            int a = 0;
            int b = 1;
            for (int i = 0; i < N; i++)
            {
                int temp = a;
                a = b;
                b = temp + b;
            }
            return a;
        }

        #endregion

        #region leafNumber accessors

        /// <summary>
        /// Retrieves the number of leaves contained in the axis.
        /// If no axis is specified, we get the cursor.
        /// </summary>
        /// <returns> The number of the leaves. </returns>
        public double GetLeafNumber(Dictionary<string, List<double>> Tillers,int axisId = 0)
        {
            if (axisId == 0)
                axisId = GetAxisId(Tillers);

            double leafNumber = GetVertexProperties(axisId)["leaf_number"];

            return leafNumber;
        }

        /// <summary>
        /// Retrieves the total of leafNumber.
        /// It sums the leafNumber of each axis composing the plant.
        /// </summary>
        /// <returns> The total number of leaves on the plant. </returns>
        public double GetPlantLeafNumber(Dictionary<string, List<double>> Tillers)
        {
            int plantId = GetPlantId();
            double totalLeafNumber = 0.0;

            int shootId = PlantHasShoot(plantId);
            List<int> axes = Components(shootId);

            foreach (int axis in axes)
                totalLeafNumber += GetLeafNumber(Tillers,axis);

            return totalLeafNumber;
        }

        /// <summary>
        /// Updates the leafNumber to the number of vertices at scale 5 (Metamers).
        /// </summary>
        void UpdateLeafNumber(Dictionary<string, List<double>> Tillers,int axisId = 0)
        {
            double leafNumber = GetLeafNumber(Tillers,axisId);

            Property("leaf_number")[axisId] = Components(axisId).Count;

        }

        /// <summary>
        /// A function which verifies that the number of leaves wanted is equal to the actual number of leaves on the plant.
        /// In case the plant has fewer leaves than the specified number, the missing leaves are added.
        /// </summary>
        /// <param name="nbLeaves"> Number of desired leaves. </param>
      /*  public void SetLeafNumber(double nbLeaves, int axisId = 0)
        {
            double fractionalPart = nbLeaves - Math.Truncate(nbLeaves);

            nbLeaves = (double)Math.Truncate(nbLeaves);

            while (nbLeaves > GetLeafNumber(axisId))
            {
                AddLeaf();
            }

            if (fractionalPart != 0)
            {
                if (axisId == 0)
                    axisId = GetAxisId();
                Property("leaf_number")[axisId] = Math.Truncate((double)(Property("leaf_number")[axisId])) + fractionalPart;
            }
        }*/

        /// <summary>
        /// Returns a list containing the identifiers of all plants.
        /// It is to note that plants are in scale number 2.
        /// </summary>
        /// <returns></returns>
        public List<int> Plants()
        {
            return Vertices(2);
        }

        #endregion

        #region Internal functions

        /// <summary>
        /// Checks if the plants already has a shoot.
        /// </summary>
        /// <param name="plantId"> The plant to verify. </param>
        /// <returns> The identifier of the shoot if found. If not, it returns zero. </returns>
        int PlantHasShoot(int plantId)
        {
            int shootExists = 0;

            if (Components(plantId).Count > 0)
            {
                foreach (int component in Components(plantId))
                {
                    if (GetVertexProperties(component)["label"].Substring(0, 5) == "shoot")
                        shootExists = component;
                }
            }
            
            return shootExists;
        }

        /// <summary>
        /// Checks if the plants already has a root.
        /// </summary>
        /// <param name="plantId"> The plant to verify. </param>
        /// <returns> The identifier of the root if found. If not, it returns zero. </returns>
        int PlantHasRoot(int plantId)
        {
            int rootExists = 0;

            if (Components(plantId).Count > 0)
            {
                foreach (int component in Components(plantId))
                {
                    if (GetVertexProperties(component)["label"].Substring(0, 4) == "root")
                        rootExists = component;
                }
            }

            return rootExists;
        }

        /// <summary>
        /// Checks if the shoot already has a mainstem or not.
        /// </summary>
        /// <param name="shootId"> Identifier of the shoot. </param>
        /// <returns> Identifier of the mainstem if found. If not, it returns zero. </returns>
        int ShootHasMainstem(int shootId)
        {
            int mainstemId = 0;

            if (Components(shootId).Count > 0)
            {
                foreach (int component in Components(shootId))
                {
                    string dummy=GetVertexProperties(component)["label"];
                    if (GetVertexProperties(component)["label"].Contains("mainstem"))
                        mainstemId = component;
                }
            }

            return mainstemId;
        }

        /// <summary>
        /// Checks if the metamer already has an internode or not.
        /// </summary>
        /// <param name="metamerId"> Identifier of the metamer. </param>
        /// <returns> Identifier of the internode if found. If not, it returns zero. </returns>
        int MetamerHasInternode(int metamerId)
        {
            int internodeId = 0;

            if (Components(metamerId).Count > 0)
            {
                foreach (int component in Components(metamerId))
                {
                    if (GetVertexProperties(component)["label"].Length > 8)
                    {
                        string label = GetVertexProperties(component)["label"].Substring(0, 9);

                        if (label == "internode")
                            internodeId = component;
                    }
                }
            }

            return internodeId;
        }

        /// <summary>
        /// Checks if the metamer already has a sheath or not.
        /// </summary>
        /// <param name="metamerId"> Identifier of the metamer. </param>
        /// <returns> Identifier of the sheath if found. If not, it returns zero. </returns>
        int MetamerHasSheath(int metamerId)
        {
            int sheathId = 0;

            if (Components(metamerId).Count > 0)
            {
                foreach (int component in Components(metamerId))
                {
                    if (GetVertexProperties(component)["label"].Substring(0, 6) == "sheath")
                        sheathId = component;
                }
            }

            return sheathId;
        }

        /// <summary>
        /// Checks if the metamer already has a blade or not.
        /// </summary>
        /// <param name="metamerId"> Identifier of the metamer. </param>
        /// <returns> Identifier of the blade if found. If not, it returns zero. </returns>
        int MetamerHasBlade(int metamerId)
        {
            int bladeId = 0;

            if (Components(metamerId).Count > 0)
            {
                foreach (int component in Components(metamerId))
                {
                    if (GetVertexProperties(component)["label"].Substring(0, 5) == "blade")
                        bladeId = component;
                }
            }

            return bladeId;
        }

        #endregion

        #region Editing functions (AddCanopy, AddPlant, AddShoot, AddRoot, AddAxis, AddMetamer, AddInternode, AddSheath, AddBlade, AddLeaf)

        /// <summary>
        /// Adds a canopy which will contain all plants.
        /// </summary>
        /// <param name="label"> Optional parameter. 
        /// Specified in case all plants are of the same botanical variety. </param>
        /// <returns> Identifier of the canopy added. </returns>
        protected int AddCanopy(string label = "canopy")
        {
            int canopy;
            Dictionary<string, dynamic> canopyLabel = new Dictionary<string, dynamic>();

            canopyLabel.Add("label", label);

            canopy = AddComponent(0, canopyLabel);

            SetCursor(canopy);

            return canopy;
        }

        /// <summary>
        /// Add a plant to the canopy.
        /// It is to note that the plant is labelled plant+number of the plant (e.g: plant0, plant1, ..).
        /// </summary>
        /// <returns> Identifier of the plant. </returns>
        protected int AddPlant()
        {

            int canopy = GetCanopyId();

            Dictionary<string,dynamic> plantLabel = new Dictionary<string,dynamic>();
            plantLabel.Add("label","plant"+nbPlants);
            plantLabel.Add("edge_type", "/");

            int plantId = AddComponent(canopy, namesValues: plantLabel);

            nbPlants++;

            SetCursor(plantId);

            return plantId;
        }

        /// <summary>
        /// Add a shoot to a plant.
        /// A naming convention would be that the shoot and the plant will have the same label number.
        /// (e.g: plant0 is decomposed into shoot0, plant1 into shoot1 and so on).
        /// </summary>
        /// <returns> The identifier of the shoot created. </returns>
        protected int AddShoot(Dictionary<string, List<double>> Tillers)
        {
            int plantId = GetPlantId();

            if (PlantHasShoot(plantId) != 0)
                plantId = AddPlant();

            string plantNb = GetVertexProperties(plantId)["label"].Substring(5);

            Dictionary<string, dynamic> shootLabel = new Dictionary<string, dynamic>();
            shootLabel.Add("label", "shoot" + plantNb);
            shootLabel.Add("edge_type", "/");

            double lastTillerFraction=1.0;
            foreach (string label in Tillers.Keys) if (lastTillerFraction > Tillers[label][2]) lastTillerFraction = Tillers[label][2];
            shootLabel.Add("last_axis_fraction", lastTillerFraction);

            int shootId = AddComponent(plantId, shootLabel);

            SetCursor(shootId);

            return shootId;

        }

        /// <summary>
        /// Add a root to a plant.
        /// A naming convention would be that the root and the plant will have the same label number.
        /// (e.g: plant0 is decomposed into root0, plant1 into root1 and so on).
        /// </summary>
        /// <returns> The identifier of the root created. </returns>
        protected int AddRoot()
        {
            int plantId = GetPlantId();

            if (PlantHasRoot(plantId) != 0)
                plantId = AddPlant();

            string plantNb = GetVertexProperties(plantId)["label"].Substring(5);

            Dictionary<string, dynamic> rootLabel = new Dictionary<string, dynamic>();
            rootLabel.Add("label", "root" + plantNb);
            rootLabel.Add("edge_type", "/");

            int rootId = AddComponent(plantId, rootLabel);

            SetCursor(rootId);

            return rootId;
             
        }

        /// <summary>
        /// Adds an axis to the plant.
        /// If the plant doesn't have a mainstem, it creates one. Its label is: "mainstem".
        /// If the plant already has one, it adds an axis on the mainstem. Its label is: "axis"+number of the axis.
        /// </summary>
        ///<param name="isAddedOnMainStem"> true, if the axes has to be added on mainstem 
        /// false, if added on a other axis. </param>
        ///<param name="parentAxisId"> Optional parameter 
        /// Id of the old axis where the new axis has to be created. </param>
        /// <returns> The identifier of the new axis added. </returns>
        private int AddAxis(Dictionary<string, List<double>> Tillers,string label="T000",bool isAddedOnMainStem = true, int parentAxisId = 0)
        {
            int axisId;

            int shootId = GetShootId(Tillers);

            int mainstemId = 0;

            if (isAddedOnMainStem) mainstemId = ShootHasMainstem(shootId);
            else mainstemId = -1;

            Dictionary<string, dynamic> axisLabel = new Dictionary<string, dynamic>();

            if (mainstemId == 0)
            {
                axisLabel.Add("label", "mainstem");
                axisLabel.Add("edge_type", "/");
                axisLabel.Add("leaf_number", 0.0);
                axisLabel.Add("axis_fraction", Tillers[label][2]);

                axisId = AddComponent(shootId, axisLabel);

            }
            else
            {
                //int axisNumber = NbChildren(mainstemId) + 1;

                axisLabel.Add("label", "axis"+axisNumber);
                axisLabel.Add("edge_type", "+");
                axisLabel.Add("leaf_number", 0.0);
                axisLabel.Add("axis_fraction", Tillers[label][2]);

                axisNumber++;
                

                if(isAddedOnMainStem || parentAxisId==0) axisId = AddChild(mainstemId, axisLabel);
                else axisId = AddChild(parentAxisId, axisLabel);
            }

            SetCursor(axisId);

            return axisId;

        }

        /// <summary>
        /// Adds a metamer to the current axis.
        /// Note that a single axis can bear multiple metamers.
        /// The new metamer is added as a component of the axis but also as a child of the last existing metamer.
        /// </summary>
        /// <returns> The identifier of the metamer. </returns>
        protected int AddMetamer(int leafLayerIndexOfFirstLeaf,int leafIndexOntheParentAxis,Dictionary<string, List<double>> Tillers,double leafFraction=1)
        {

            int axisId = GetAxisId(Tillers);

            // Retrieve the number of the last metamer to label it.
            int metamerNbOnAxis = Components(axisId).Count;

            // Identifier of the last metamer (if the axis already bears other metamers).
            int lastMetamer = 0;
            if (metamerNbOnAxis != 0)
            {
                lastMetamer = Components(axisId).Max();
            }

            Dictionary<string, dynamic> metamerLabel = new Dictionary<string, dynamic>();
            metamerLabel.Add("label", "metamer" + metamerNb);
            metamerLabel.Add("edge_type", "/");
            metamerLabel.Add("leaf_fraction", leafFraction);

            int metamerId = AddComponent(axisId, metamerLabel);
            
            if (lastMetamer != 0)
            {
                metamerLabel["edge_type"] = "<";
                AddChild(lastMetamer, metamerLabel, metamerId);
            }
            metamerNb++;
            SetCursor(metamerId);
            UpdateLeafNumber(Tillers,axisId);

            return metamerId;
        }

        /// <summary>
        /// Adds an internode to the current metamer.
        /// There is only one internode per metamer, and it is on scale 6. (The internode is a component of the metamer).
        /// </summary>
        /// <returns> Identifier of the internode. </returns>
        protected int AddInternode(int leafLayerIndexOfFirstLeaf, int leafIndexOntheParentAxis,Dictionary<string, List<double>> Tillers, double leafFraction = 1)
        {
            int metamerId = GetMetamerId(leafLayerIndexOfFirstLeaf, leafIndexOntheParentAxis, Tillers);

            // Retrieve the identifier of the internode if it already exists.
            int internodeId = MetamerHasInternode(metamerId);
            if (internodeId != 0)
                metamerId = AddMetamer(leafLayerIndexOfFirstLeaf, leafIndexOntheParentAxis, Tillers);

            Dictionary<string, dynamic> internodeLabel = new Dictionary<string, dynamic>();
            internodeLabel.Add("label", "internode");
            internodeLabel.Add("edge_type", "/");
            //internodeLabel.Add("leaf_fraction", leafFraction);

            foreach (string prop in propertiesToAdd.Keys)
            {


                if (prop.Split('_')[0] == "internode")
                {

                    double value = DistributeLeafLayerPropertyValues(propertiesToAdd[prop], leafLayerIndexOfFirstLeaf, leafIndexOntheParentAxis, leafFraction, false);
                    internodeLabel.Add(prop, value);
                }
            }

            internodeId = AddComponent(metamerId, internodeLabel);

            SetCursor(internodeId);

            return internodeId;
        }

        /// <summary>
        /// Adds a sheath to the current metamer.
        /// It is to note that a sheath requires that an internode already exists.
        /// The sheath is a child of the internode and a component of the metamer.
        /// </summary>
        /// <returns> Identifier of the sheath added. </returns>
        protected int AddSheath(int leafLayerIndexOfFirstLeaf, int leafIndexOntheParentAxis, Dictionary<string, List<double>> Tillers, double leafFraction = 1)
        {
            int metamerId = GetMetamerId(leafLayerIndexOfFirstLeaf, leafIndexOntheParentAxis, Tillers);
            
            // Verifies that the metamer doesn't already have a sheath. If so, it creates a new metamer.
            int sheathId = MetamerHasSheath(metamerId);
            if (sheathId != 0)
                metamerId = AddMetamer(leafLayerIndexOfFirstLeaf, leafIndexOntheParentAxis, Tillers);

            int internodeId = MetamerHasInternode(metamerId);

            if (internodeId == 0)
                internodeId = AddInternode(leafLayerIndexOfFirstLeaf, leafIndexOntheParentAxis, Tillers);

            Dictionary<string, dynamic> sheathLabel = new Dictionary<string, dynamic>();
            sheathLabel.Add("label", "sheath");
            sheathLabel.Add("edge_type", "/");
            //sheathLabel.Add("leaf_fraction", leafFraction);



            foreach (string prop in propertiesToAdd.Keys)
            {
                if (prop.Split('_')[0] == "sheath")
                {
                    double value = DistributeLeafLayerPropertyValues(propertiesToAdd[prop], leafLayerIndexOfFirstLeaf, leafIndexOntheParentAxis, leafFraction);
                    sheathLabel.Add(prop, value);
                }
            }

            sheathId = AddComponent(metamerId, sheathLabel);

            sheathLabel["edge_type"] = "+";
            AddChild(internodeId, sheathLabel, sheathId);

            SetCursor(sheathId);

            return sheathId;
        }

        /// <summary>
        /// Adds a blade to the current metamer.
        /// It is to note that a blade requires that a sheath already exists.
        /// The blade is a child of the sheath, and a component of the metamer.
        /// </summary>
        ///<param name="leafLayerIndexOfFirstLeaf">Leaf Layer index</param>
        /// <returns> Identifier of the blade added. </returns>
        protected int AddBlade(int leafLayerIndexOfFirstLeaf, int leafIndexOntheParentAxis, Dictionary<string, List<double>> Tillers,double leafFraction=1)
        {
            int metamerId = GetMetamerId(leafLayerIndexOfFirstLeaf, leafIndexOntheParentAxis, Tillers);

            // Verifies that the metamer doesn't already have a blade. If so, it creates a new metamer.
            int bladeId = MetamerHasBlade(metamerId);
            if (bladeId != 0)
                metamerId = AddMetamer(leafLayerIndexOfFirstLeaf, leafIndexOntheParentAxis, Tillers);

            int internodeId = MetamerHasInternode(metamerId);
            int sheathId = MetamerHasSheath(metamerId);

            if (sheathId == 0)
                sheathId = AddSheath(leafLayerIndexOfFirstLeaf, leafIndexOntheParentAxis,Tillers);

            Dictionary<string, dynamic> bladeLabel = new Dictionary<string, dynamic>();
            bladeLabel.Add("label", "blade");
            bladeLabel.Add("edge_type", "/");
            //bladeLabel.Add("leaf_fraction", leafFraction);

            foreach (string prop in propertiesToAdd.Keys)
            {   
                if (prop.Split('_')[0] == "lamina")
                {

                    double value = DistributeLeafLayerPropertyValues(propertiesToAdd[prop], leafLayerIndexOfFirstLeaf, leafIndexOntheParentAxis, leafFraction);
                    bladeLabel.Add(prop, value);
                }
            }
            bladeId = AddComponent(metamerId, bladeLabel);

            bladeLabel["edge_type"] = "<";
            AddChild(sheathId, bladeLabel, bladeId);

            SetCursor(bladeId);

            return bladeId;
        }

        /// <summary>
        /// A function which summarizes the act of adding a leaf to the plant.
        /// This requires to add a metamer, then the internode, sheath and blade which compose it.
        /// It is to note that the leafNumber is implicitly updated in the function AddMetamer.
        /// </summary>
        public void AddLeaf(int leafLayerIndexOfFirstLeaf, int leafIndexOntheParentAxis, Dictionary<string, List<double>> Tillers,double leafFraction=1)
        {


            AddMetamer(leafLayerIndexOfFirstLeaf, leafIndexOntheParentAxis, Tillers, leafFraction);
            AddInternode(leafLayerIndexOfFirstLeaf, leafIndexOntheParentAxis, Tillers, leafFraction);
            AddSheath(leafLayerIndexOfFirstLeaf, leafIndexOntheParentAxis, Tillers, leafFraction);
            AddBlade(leafLayerIndexOfFirstLeaf, leafIndexOntheParentAxis, Tillers, leafFraction);
        }

        public void AddTillersAndLeaves(Dictionary<string, List<double>> Tillers)
        {
            int OldnOrder=0;
            int nOrder=0;
            int OldposMainStem=0;
            int posMainStem=0;
            int posParentxis=0;
            int count = 0;
            List<List<int>> FirstAxe = new List<List<int>>();



            int MainStemId = AddAxis(Tillers);//Add the main stem


            double leafFraction = 1;

            for (int i = 0; i < Math.Ceiling(Tillers["T000"][1]); i++)
            {
                if (i+1 == Math.Ceiling(Tillers["T000"][1])) leafFraction = Tillers["T000"][2];   

                AddLeaf(0, i, Tillers, leafFraction);

            }
            int deltaOnMainStem = 0;

            int vertexId = 0;

            

            foreach(string tiller in Tillers.Keys){

                if (tiller == "T000") continue;

                leafFraction = Tillers[tiller][2];

                nOrder = Convert.ToInt32((tiller.Substring(1, 1)));
                OldposMainStem=posMainStem;
                posMainStem = Convert.ToInt32((tiller.Substring(2, 1)));
                posParentxis = Convert.ToInt32((tiller.Substring(3, 1)));


                if (nOrder == 1)
                {
                    AddAxis(Tillers, tiller, true);
                    for (int i = 0; i < (int)Tillers[tiller][1]; i++) AddLeaf((int)Tillers[tiller][0] - 1, i, Tillers, leafFraction);

                    List<int> list=new List<int>();
                    list.Add(MainStemId + 1);
                    FirstAxe.Add(list);
                }
                else
                {

                    if(nOrder!=OldnOrder){

                        vertexId = FirstAxe[count][0];
                        vertexId = AddAxis(Tillers, tiller,false, vertexId);
                        for (int i = 0; i < (int)Tillers[tiller][1]; i++) AddLeaf((int)Tillers[tiller][0] - 1, i, Tillers, leafFraction);
                        count++;
                        deltaOnMainStem = 0;

                        List<int> list = new List<int>();
                        list.Add(vertexId);
                        FirstAxe.Add(list);
                        
                    }
                    else if (posMainStem == OldposMainStem)
                    {


                        vertexId = AddAxis(Tillers, tiller,false, vertexId);
                        for (int i = 0; i < (int)Tillers[tiller][1]; i++) AddLeaf((int)Tillers[tiller][0] - 1, i, Tillers, leafFraction);
                        if (nOrder == 2) vertexId = FirstAxe[count][deltaOnMainStem];

                    }
                    else{
                        deltaOnMainStem++;
                        if (nOrder > 2) vertexId = FirstAxe[count-1][deltaOnMainStem];
                        else vertexId = MainStemId + deltaOnMainStem + 1;

                        vertexId = AddAxis(Tillers, tiller,false, vertexId);
                        for (int i = 0; i < (int)Tillers[tiller][1]; i++) AddLeaf((int)Tillers[tiller][0] - 1, i, Tillers, leafFraction);

                        
                        //FirstAxe[count][deltaOnMainStem] = vertexId;
                        List<int> list = new List<int>();
                        list.Add(vertexId);
                        FirstAxe.Add(list);
                        
                    
                    }
                }


            }

        }

        #endregion

        #region High level functions (Wheat)

        /// <summary>
        /// A function that the final user can use to create a Wheat plant composed of a number of leaves they will choose.
        /// </summary>
        /// <param name="NbLeaves"> The number of leaves desired in the plant. </param>
        /// <returns> The plant structure. </returns>
        public void CreateBasicWheat()
        {
            this.AddCanopy("wheat");
            this.AddPlant();
            this.AddRoot();
            this.AddShoot(this.tillerResults);
            this.AddTillersAndLeaves(this.tillerResults);

        }

        public void SaveToFile(string path,bool isJSONSaved)
        {
            traversal t = new traversal();

            if (isJSONSaved) t.SaveToJSONFile(this, path, true);
            else t.SaveToFile(this, path, true);
        }

        #endregion

  /*      #region Testing functions

        public int TestAddCanopy(string label = "canopy")
        {
            return AddCanopy(label);
        }

        public int TestAddPlant()
        {
            return AddPlant();
        }

        public int TestAddShoot()
        {
            return AddShoot();
        }

        public int TestAddRoot()
        {
            return AddRoot();
        }

        public int TestAddAxis()
        {
            return AddAxis();
        }

        public int TestAddMetamer()
        {
            return AddMetamer();
        }

        public int TestAddInternode()
        {
            return AddInternode();
        }

        public int TestAddSheath()
        {
            return AddSheath();
        }

        public int TestAddBlade()
        {
            return AddBlade();
        }

        #endregion
        
        static void Main(String[] args)
        {

        }*/

    }
}
