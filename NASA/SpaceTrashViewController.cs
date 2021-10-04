using ARKit;
using CoreGraphics;
using SceneKit;
using SkiaSharp;
using SkiaSharp.TextBlocks;
using SkiaSharp.Views.iOS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UIKit;

namespace XamarinArkitSample
{
    public partial class SpaceTrashViewController : UIViewController
    {
        private readonly ARSCNView sceneView;
        Random random = new Random();
        SCNNode masterNode;
        double speedUpFactor = 100;

        List<OrbitItemNode> orbitItems = new List<OrbitItemNode>();
        List<UIButton> uiButtons = new List<UIButton>();
        List<UIButton> uiSpeedButtons = new List<UIButton>();

        bool isActiveSelected = true;
        bool isWeatherSelected = true;
        bool isSpaceStationSelected = true;
        bool isCosmosDebrisSelected = true;

        public SpaceTrashViewController()
        {
            this.sceneView = new ARSCNView {
                AutoenablesDefaultLighting = true
            };

            this.View.AddSubview(this.sceneView);
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            this.sceneView.Frame = this.View.Frame;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            this.sceneView.Session.Run(new ARWorldTrackingConfiguration
            {
                AutoFocusEnabled = false,
                LightEstimationEnabled = true,
                WorldAlignment = ARWorldAlignment.Gravity
            }, ARSessionRunOptions.ResetTracking | ARSessionRunOptions.RemoveExistingAnchors);

            var random = new Random();
            masterNode = new SCNNode();
            masterNode.Position = new SCNVector3(0, 0, -1f); 

            // Setup Touch Gesture handlers
            var doubleTapGesture = new UITapGestureRecognizer(HandleDoubleTapGesture);
            doubleTapGesture.NumberOfTapsRequired = 2;
            this.sceneView.AddGestureRecognizer(doubleTapGesture);

            var tapGesture = new UITapGestureRecognizer(HandleTapGesture);
            tapGesture.NumberOfTapsRequired = 1;
            tapGesture.RequireGestureRecognizerToFail(doubleTapGesture);
            this.sceneView.AddGestureRecognizer(tapGesture);

            var pinchGesture = new UIPinchGestureRecognizer(HandlePinchGesture);
            this.sceneView.AddGestureRecognizer(pinchGesture);

            var rotateGesture = new UIRotationGestureRecognizer(HandleRotateGesture);
            this.sceneView.AddGestureRecognizer(rotateGesture);

            // Add Planet Earth
            var worldNode = new WorldNode(size:0.1f); // 10cm radius
            worldNode.ChangeRotateSpeed(speedUpFactor);
            masterNode.Add(worldNode);

            // Add dark box
            var darkMaterial = new SCNMaterial();
            darkMaterial.Diffuse.Contents = UIColor.Black;
            darkMaterial.DoubleSided = true;

            var darkBox = new SCNNode();
            darkBox.Geometry = SCNBox.Create(10f, 10f, 10f, 0f);
            darkBox.Geometry.FirstMaterial = darkMaterial;
            darkBox.Opacity = 0.97f; // To dark and doesn't look augmented, too light and cannot see nodes

            this.sceneView.Scene.RootNode.AddChildNode(darkBox);

            UIColor unselectedGray = UIColor.FromRGBA(50, 50, 50, 200);
            UIColor selectedGray = UIColor.FromRGBA(100, 100, 100, 50);


            // Add buttons to UI
            UIButton speed100Button = CreateButton("100x", unselectedGray, new CGRect(10, 100, 80, 50));
            speed100Button.TouchUpInside += (sender, e) => {
                speedUpFactor = 100;

                foreach (var item in orbitItems) {
                    item.StartRotation(speedUpFactor);
                }

                worldNode.ChangeRotateSpeed(speedUpFactor);
                HighlightSpeedButton(speed100Button);

            };
            this.View.Add(speed100Button);

            UIButton speed200Button = CreateButton("200x", unselectedGray, new CGRect(100, 100, 80, 50));
            speed200Button.TouchUpInside += (sender, e) => {
                speedUpFactor = 200;

                foreach (var item in orbitItems){
                    item.StartRotation(speedUpFactor);
                }

                worldNode.ChangeRotateSpeed(speedUpFactor);
                HighlightSpeedButton(speed200Button);

            };
            this.View.Add(speed200Button);

            UIButton speed500Button = CreateButton("500x", unselectedGray, new CGRect(190, 100, 80, 50));
            speed500Button.TouchUpInside += (sender, e) => {
                speedUpFactor = 500;

                foreach (var item in orbitItems) {
                    item.StartRotation(speedUpFactor);
                }

                worldNode.ChangeRotateSpeed(speedUpFactor);
                HighlightSpeedButton(speed500Button);

            };
            this.View.Add(speed500Button);

            UIButton speed1000Button = CreateButton("1000x", unselectedGray, new CGRect(280, 100, 80, 50));
            speed1000Button.TouchUpInside += (sender, e) => {
                speedUpFactor = 1000;

                foreach (var item in orbitItems) {
                    item.StartRotation(speedUpFactor);
                }

                worldNode.ChangeRotateSpeed(speedUpFactor);
                HighlightSpeedButton(speed1000Button);

            };
            this.View.Add(speed1000Button);

            uiButtons.Add(speed100Button);
            uiButtons.Add(speed200Button);
            uiButtons.Add(speed500Button);
            uiButtons.Add(speed1000Button);

            uiSpeedButtons.Add(speed100Button);
            uiSpeedButtons.Add(speed200Button);
            uiSpeedButtons.Add(speed500Button);
            uiSpeedButtons.Add(speed1000Button);

            AddItems("active-satellites.csv", "active");
            AddItems("weather-satellites.csv", "weather");
            AddItems("space-stations.csv", "space-station");
            AddItems("cosmos-2251-debris.csv", "cosmos-2251-debris");

            // Add DataSet buttons
            UIButton activeSatelliteButton = CreateButton($"Active ({orbitItems.Count(x => x.Type == "active")})", UIColor.Green.ColorWithAlpha(0.5f), new CGRect(10, 680, 170, 50));
            UIButton weatherSatelliteButton = CreateButton($"Weather ({orbitItems.Count(x => x.Type == "weather")})", UIColor.Yellow.ColorWithAlpha(0.5f), new CGRect(200, 680, 170, 50));
            UIButton spaceStationButton = CreateButton($"Space Station ({orbitItems.Count(x => x.Type == "space-station")})", UIColor.Blue.ColorWithAlpha(0.5f), new CGRect(10, 750, 170, 50));
            UIButton cosmos2251DebrisButton = CreateButton($"Cosmos 2251 Debris ({orbitItems.Count(x => x.Type == "cosmos-2251-debris")})", UIColor.Red.ColorWithAlpha(0.5f), new CGRect(200, 750, 170, 50));

            // todo: Refactor these
            activeSatelliteButton.TouchUpInside += (sender, e) => {
                ToggleItemType("active");

                if (!isActiveSelected)
                {
                    activeSatelliteButton.BackgroundColor = UIColor.Green.ColorWithAlpha(0.5f);
                }
                else
                {
                    activeSatelliteButton.BackgroundColor = UIColor.Green.ColorWithAlpha(0.2f);
                }

                isActiveSelected = !isActiveSelected;
            };

            weatherSatelliteButton.TouchUpInside += (sender, e) => {
                ToggleItemType("weather");

                if (!isWeatherSelected)
                {
                    weatherSatelliteButton.BackgroundColor = UIColor.Yellow.ColorWithAlpha(0.5f);
                }
                else
                {
                    weatherSatelliteButton.BackgroundColor = UIColor.Yellow.ColorWithAlpha(0.2f);
                }

                isWeatherSelected = !isWeatherSelected;
            };

            spaceStationButton.TouchUpInside += (sender, e) => {
                ToggleItemType("space-station");

                if (!isSpaceStationSelected)
                {
                    spaceStationButton.BackgroundColor = UIColor.Blue.ColorWithAlpha(0.5f);
                }
                else
                {
                    spaceStationButton.BackgroundColor = UIColor.Blue.ColorWithAlpha(0.2f);
                }

                isSpaceStationSelected = !isSpaceStationSelected;
            };

            cosmos2251DebrisButton.TouchUpInside += (sender, e) => {
                ToggleItemType("cosmos-2251-debris");

                if(!isCosmosDebrisSelected) {
                    cosmos2251DebrisButton.BackgroundColor = UIColor.Red.ColorWithAlpha(0.5f);
                }
                else {
                    cosmos2251DebrisButton.BackgroundColor = UIColor.Red.ColorWithAlpha(0.2f);
                }

                isCosmosDebrisSelected = !isCosmosDebrisSelected;
            };

            // Add My Handle
            UIButton leeButton = CreateButton("@LeeEnglestone", UIColor.Black.ColorWithAlpha(0), new CGRect(50, 600, 300, 50));

            uiButtons.Add(activeSatelliteButton);
            uiButtons.Add(weatherSatelliteButton);
            uiButtons.Add(spaceStationButton);
            uiButtons.Add(cosmos2251DebrisButton);
            uiButtons.Add(leeButton);

            this.View.Add(activeSatelliteButton);
            this.View.Add(weatherSatelliteButton);
            this.View.Add(spaceStationButton);
            this.View.Add(cosmos2251DebrisButton);
            this.View.Add(leeButton);

            HighlightSpeedButton(speed100Button);

            this.sceneView.Scene.RootNode.AddChildNode(masterNode);
        }

        private void ToggleItemType(string type)
        {
            foreach(var item in orbitItems)
            {
                if(item.Type == type)
                {
                    if(item.Opacity == 0)
                    {
                        item.Show();
                    }
                    else
                    {
                        item.Hide();
                    }
                }
            }
        }

        private UIButton CreateButton(string text, UIColor backgroundColor, CGRect rect)
        {
            UIButton uiButton = new UIButton(UIButtonType.System);
            uiButton.Frame = rect;
            uiButton.SetTitle(text, UIControlState.Normal);
            uiButton.BackgroundColor = backgroundColor;
            uiButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            uiButton.Layer.CornerRadius = 5f;
            uiButton.TitleLabel.AdjustsFontSizeToFitWidth = true;
            uiButton.TitleLabel.LineBreakMode = UILineBreakMode.Clip;
            return uiButton;
        }

        private void AddItems(string filename, string type)
        {
            orbitItems.AddRange(GetItemsFromFile(filename, type));

            foreach (var orbitItemNode in orbitItems)
            {
                orbitItemNode.StartRotation(speedUpFactor);

                // Rotate to match inclination
                var parentNodeToRotateAxis = new SCNNode();
                parentNodeToRotateAxis.AddChildNode(orbitItemNode);

                var inclinationAngle = ConvertInclinationToAngle(orbitItemNode.InclinationDegrees);

                parentNodeToRotateAxis.EulerAngles = new SCNVector3(-inclinationAngle, 0, 0);

                masterNode.Add(parentNodeToRotateAxis);
            }
        }

        private void HighlightSpeedButton(UIButton button)
        {
            // Iterate through speed buttons
            foreach(var speedButton in uiSpeedButtons)
            {
                speedButton.BackgroundColor = UIColor.FromRGBA(50, 50, 50, 200);
                speedButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            }

            // Set this to selected
            button.BackgroundColor = UIColor.White;
            button.SetTitleColor(UIColor.Black, UIControlState.Normal);
        }


        private float CalculateHeightKm(double rotationsPerDay)
        {
            const double gravitationalConstant = 6.673e-11;
            const double earthRadiusM = 6371000;
            const double earthMassKg = 5.972e24;

            var orbitalPeriodS = (24 * 60 * 60)/rotationsPerDay;
            
            var heightM = Math.Cbrt(
                    (Math.Pow(orbitalPeriodS, 2) * gravitationalConstant * earthMassKg)
                    /
                    (4 * Math.Pow(Math.PI, 2))
                    ) - earthRadiusM;

            var heightKm = heightM / 1000;

            return (float)heightKm;
        }

        private void HandlePinchGesture(UIPinchGestureRecognizer sender)
        {
            var node = masterNode;

            var scaleX = (float)sender.Scale * node.Scale.X;
            var scaleY = (float)sender.Scale * node.Scale.Y;
            var scaleZ = (float)sender.Scale * node.Scale.Z;

            node.Scale = new SCNVector3(scaleX, scaleY, scaleZ);
            sender.Scale = 1;
        }

       

        float currentAngleZ;
        float newAngleZ;

        private void HandleRotateGesture(UIRotationGestureRecognizer sender)
        {
            var areaTouched = sender.View as SCNView;
            var location = sender.LocationInView(areaTouched);
            var hitTestResults = areaTouched.HitTest(location, new SCNHitTestOptions());

            var hitTest = hitTestResults.FirstOrDefault();

            if (hitTest == null)
                return;

            var node = masterNode;

            newAngleZ = (float)(-sender.Rotation);
            newAngleZ += currentAngleZ;
            node.EulerAngles = new SCNVector3(node.EulerAngles.X, newAngleZ, node.EulerAngles.Z);
        }

        private void HandleDoubleTapGesture(UITapGestureRecognizer sender)
        {
            // Toggle UI buttons
            foreach(var uiButton in uiButtons)
            {
                uiButton.Hidden = !uiButton.Hidden;
            }


            //var areaTapped = sender.View as SCNView;
            //var location = sender.LocationInView(areaTapped);
            //var hitTestResults = areaTapped.HitTest(location, new SCNHitTestOptions());


        }

        private void HandleTapGesture(UITapGestureRecognizer sender)
        {
            var areaTapped = sender.View as SCNView;
            var location = sender.LocationInView(areaTapped);
            var hitTestResults = areaTapped.HitTest(location, new SCNHitTestOptions());

            var hitTest = hitTestResults.FirstOrDefault();

            if (hitTest == null)
                return;

            if (!(hitTest.Node.ParentNode is OrbitItemNode))
                return;

            OrbitItemNode node = (OrbitItemNode)hitTest.Node.ParentNode;

            var text = $"Id:{node.Id}\r\nInclination:{node.InclinationDegrees}\r\nRight Ascention:{node.RightAscention}\r\nHeight:{node.Height}\r\nRadius:{node.Radius}\r\nRotations Per Day:{node.RotationsPerDay}";

            node.ShowDetails();


            //Create Alert
            //var okCancelAlertController = UIAlertController.Create(node.Name, text, UIAlertControllerStyle.Alert);

            //Add Actions
            //okCancelAlertController.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, alert => Console.WriteLine("Okay was clicked")));
            //okCancelAlertController.AddAction(UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, alert => Console.WriteLine("Cancel was clicked")));

            //Present Alert
            //PresentViewController(okCancelAlertController, true, null);

        }

        public float ConvertToRadians(float angle)
        {
            var radians = (float)((Math.PI * angle) / 180);

            //Debug.WriteLine($"Angle:{angle}, Radians:{radians}");

            return radians;
        }

        private float ConvertInclinationToAngle(float inclinationDegrees)
        {
            // 0 = Equator

            // 90 = To north pole

            // > 90 = starting to go counter clockwise

            return ConvertToRadians(inclinationDegrees);
        }

        private float ConvertHeightToRadius(float heightInKm)
        {      
            // Convert to factor
            //6371 = 0.1;
            float factor = 0.00001569612f; 

            // Return default for now..
            return (heightInKm * factor) + 0.1f;
        }

        private double GetRandomDurationInSeconds()
        {
            return random.NextDouble() * 100;
        }

        private float GetRandomRadius()
        {
            int radiusOfEarth = 6371;

            float factor = 0.00001569612f;

            // Random!
            float height = radiusOfEarth + random.Next(166, 150_000);

            // Return default for now..
            return height * factor;
        }

        public List<OrbitItemNode> GetItemsFromFile(string filename, string type)
        {
            var items = new List<OrbitItemNode>();

            try{
                string[] lines = System.IO.File.ReadAllLines($"NASA/{filename}");

                foreach (var line in lines.Skip(1).ToArray())
                {
                    var parts = line.Split(",", StringSplitOptions.RemoveEmptyEntries);
                    var name = parts[0];
                    var id = parts[1];
                    var rotationsPerDay = float.Parse(parts[3]);
                    var inclination = float.Parse(parts[5]);
                    var rightOfAscention = float.Parse(parts[6]);
                    var height = CalculateHeightKm(rotationsPerDay);
                    var radius = ConvertHeightToRadius(height);


                    // x = left (-) to right (+)
                    // y = down (-) to up )+)
                    // z = forward (-) to backward (+)

                    float x = 0;
                    float y = 0;
                    float z = 0;


                    // Plot position on its orbit based on its Right Ascention
                    // v1
                    x = radius * (float)Math.Sin(Math.PI * 2 * rightOfAscention / 360);
                    z = radius * (float)Math.Cos(Math.PI * 2 * rightOfAscention / 360);

                    // v2
                    //x = radius * (float)Math.Cos(inclination) * (float)Math.Cos(rightOfAscention);
                    //z = radius * (float)Math.Cos(inclination) * (float)Math.Sin(rightOfAscention);

                    /*
                     * x = radius * Math.Cos(right
                     𝑥=𝑟cos𝛿cos𝛼
                    𝑦=𝑟cos𝛿sin𝛼
                     * */


                    var item = new OrbitItemNode(new SCNVector3(x, 0, z), type);

                    // May need to move Y for inclination??


                    item.InclinationDegrees = inclination;
                    item.Name = name;
                    item.Id = id;
                    item.RotationsPerDay = rotationsPerDay;
                    item.RightAscention = rightOfAscention;
                    item.Height = height;
                    item.Radius = radius;

                    items.Add(item);

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception!");
                Debug.WriteLine(ex.Message);
            }

            return items;
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            this.sceneView.Session.Pause();
        }
    }

    public class OrbitItemNode : SCNNode
    {
        public string InternationalDesignator { get; set; }
        public string NoradCatalogueNumber { get; set; }
        public string Name { get; set; }
        public string Id { get; set; }
        public float PeriodMinutes { get; set; }
        public float InclinationDegrees { get; set; }
        public float RightAscention { get; set; }
        public float Height { get; set; }
        public float RotationsPerDay { get; set; }
        public string Type { get; set; }

        public bool Selected { get; set; }

        SCNAction fadeInAction;
        SCNAction fadeOutAction;

        SCNNode itemNode;
        SCNNode torusNode;
        
        public float Radius { get; set; }

        public OrbitItemNode(SCNVector3 position, string type)
        {
            Type = type;

            itemNode = new SCNNode();

            var material = new SCNMaterial();

            switch(type)
            {
                case "active":
                    material.Diffuse.Contents = UIColor.Green;
                    break;

                case "weather":
                    material.Diffuse.Contents = UIColor.Yellow;
                    break;

                case "space-station":
                    material.Diffuse.Contents = UIColor.Blue;
                    break;

                case "cosmos-2251-debris":
                    material.Diffuse.Contents = UIColor.Red;
                    break;

                default:
                    material.Diffuse.Contents = UIColor.Gray;
                    break;
            }


            var geometry = SCNSphere.Create(0.0005f); 
            geometry.FirstMaterial = material;

            itemNode.Geometry = geometry;
            itemNode.Position = position;

            fadeInAction = SCNAction.FadeIn(1);
            fadeOutAction = SCNAction.FadeOut(1);

            this.AddChildNode(itemNode);
        }

        public void Show()
        {
            this.RunAction(fadeInAction);
        }

        public void Hide()
        {
            this.RunAction(fadeOutAction);
        }

        public void StartRotation(double speedUpFactor)
        {
            var rotateDirection = new SCNVector3(0, 1, 0);

            double durationInSeconds = (24 / this.RotationsPerDay) * 60 * 60;
            durationInSeconds = durationInSeconds / speedUpFactor;

            this.RemoveAllActions();

            var rotateAction = SCNAction.RotateBy((float)Math.PI, rotateDirection, durationInSeconds);
            var repeatForever = SCNAction.RepeatActionForever(rotateAction);
            this.RunAction(repeatForever);
        }

        public void ShowDetails()
        {
            // Label node
            var details = $"Id: {this.Id}\r\nInclination: {this.InclinationDegrees} degrees\r\nDaily Rotations: {this.RotationsPerDay} \r\nHeight: {this.Height} km";

            var detailsMaterial = new SCNMaterial();
            detailsMaterial.Diffuse.Contents = GetItemLabelImage(Name, details);

            var labelNode = new SCNNode();
            labelNode.Geometry = SCNPlane.Create(0.04f, 0.02f);
            ((SCNPlane)labelNode.Geometry).CornerRadius = 0.002f;
            labelNode.Geometry.FirstMaterial = detailsMaterial;
            labelNode.Constraints = new[] { new SCNBillboardConstraint() };
            labelNode.Opacity = 0.7f;
            labelNode.Position = new SCNVector3(0, 0.05f, 0);

            itemNode.AddChildNode(labelNode);

            // Line to label node
            var line = DrawLineNode(this.Position, labelNode.Position, UIColor.White);
            itemNode.AddChildNode(line);


            // Torus node
            torusNode = new SCNNode();
            torusNode.Opacity = 0.7f;
            torusNode.Geometry = SCNTorus.Create(Radius, 0.0001f);

            var torusMaterial = new SCNMaterial();
            torusMaterial.Diffuse.Contents = UIColor.Yellow;
            this.AddChildNode(torusNode);

            // Make the selected item yellow?


        }

        private UIImage GetNameImage()
        {
            var fontNameBold = new Font(30, true);

            using (var Surface = SKSurface.Create(new SKImageInfo(width: 400, 200, SKImageInfo.PlatformColorType, SKAlphaType.Premul)))
            {
                var canvas = Surface.Canvas;
                canvas.Clear(SKColors.White);

                var rectName = new SKRect(20, 50, 380, 0);
                var textName = new TextBlock(fontNameBold, SKColors.White, "@LeeEnglestone", SkiaSharp.TextBlocks.Enum.LineBreakMode.Center);

                canvas.DrawTextBlock(textName, rectName);

                return Surface.Snapshot().ToUIImage();
            }
        }

        private UIImage GetItemLabelImage(string name, string details)
        {
            var fontNameBold = new Font(30, true);
            var fontDescription = new Font(20, false);

            // Create Skiasharp image
            using (var Surface = SKSurface.Create(new SKImageInfo(width: 400, 200, SKImageInfo.PlatformColorType, SKAlphaType.Premul)))
            {
                var canvas = Surface.Canvas;
                canvas.Clear(SKColors.White);

                // Name
                var rectName = new SKRect(0, 10, 380, 0);
                var textName = new TextBlock(fontNameBold, SKColors.Black, name, SkiaSharp.TextBlocks.Enum.LineBreakMode.Center);

                // Details
                var rectDescription = new SKRect(10, 70, 380, 0);
                var textDescription = new TextBlock(fontDescription, SKColors.Black, details, SkiaSharp.TextBlocks.Enum.LineBreakMode.Center);

                canvas.DrawTextBlock(textName, rectName);
                canvas.DrawTextBlock(textDescription, rectDescription);

                return Surface.Snapshot().ToUIImage();
            }
        }

        public SCNNode DrawLineNode(SCNVector3 pointA, SCNVector3 pointB, UIColor color)
        {
            float lineRadius = 0.0001f;
            int radialSegments = 10;

            var distance = DistanceBetweenPoints(pointA, pointB);
            var line = DrawCylinderBetweenPoints(pointA, pointB, distance, lineRadius, radialSegments, color);

            line.Look(pointB, this.ParentNode.ParentNode.ParentNode.WorldUp, line.WorldUp);

            return line;
        }

        public static SCNNode DrawCylinderBetweenPoints(SCNVector3 a, SCNVector3 b, nfloat length, nfloat radius, int radialSegments, UIColor color)
        {
            var material = new SCNMaterial();
            material.Diffuse.Contents = color;

            SCNNode cylinderNode;
            SCNCylinder cylinder = new SCNCylinder();
            cylinder.Radius = radius;
            cylinder.Height = length;
            cylinder.RadialSegmentCount = radialSegments;
            cylinderNode = SCNNode.FromGeometry(cylinder);
            cylinderNode.Position = GetMidpoint(a, b);
            cylinderNode.Geometry.FirstMaterial = material;
            cylinderNode.Opacity = 0.5f;

            return cylinderNode;
        }

        public static nfloat DistanceBetweenPoints(SCNVector3 a, SCNVector3 b)
        {
            SCNVector3 vector = new SCNVector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
            return (nfloat)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z);
        }

        public static SCNVector3 GetMidpoint(SCNVector3 a, SCNVector3 b)
        {
            float x = (a.X + b.X) / 2;
            float y = (a.Y + b.Y) / 2;
            float z = (a.Z + b.Z) / 2;

            return new SCNVector3(x, y, z);
        }
    }

    public class WorldNode : SCNNode
    {
        public WorldNode(float size)
        {
            var rootNode = new SCNNode
            {
                Geometry = CreateGeometry(size),
                Opacity = 0.99f
            };

            AddChildNode(rootNode);
        }

        public void ChangeRotateSpeed(double speedUpFactor)
        {
            this.RemoveAllActions();

            var rotateWorldDurationS = 24 * 60 * 60 / speedUpFactor;
            var worldRotateAction = SCNAction.RotateBy(0, (float)(Math.PI), 0, rotateWorldDurationS);
            var worldRepeatForever = SCNAction.RepeatActionForever(worldRotateAction);
            this.RunAction(worldRepeatForever);
        }

        private static SCNGeometry CreateGeometry(float size)
        {
            var material = new SCNMaterial();
            material.Diffuse.Contents = UIImage.FromFile("Images/world-map.jpg");
            material.DoubleSided = true;

            var geometry = SCNSphere.Create(size);
            geometry.Materials = new[] { material };

            return geometry;
        }
    }
}