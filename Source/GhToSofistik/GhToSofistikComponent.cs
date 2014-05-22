﻿// GrassHopper To Sofistik component for GrassHopper
// Convert a karamba model to a .dat file readable by Sofistik
// Git: https://github.com/AlbericTrancart/GhToSofistik
// Contact: alberic.trancart@eleves.enpc.fr

using System;
using System.Collections.Generic;
using System.IO;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Karamba.Models;
using Karamba.Elements;

using GhToSofistik.Classes;

namespace GhToSofistik {
    public class GhToSofistikComponent : GH_Component {
        // Component configuration
        public GhToSofistikComponent() : base("GhToSofistik", "GtS", "Convert Karamba model to a .dat file readable by Sofistik", "Karamba", "Extra") {}

        // Registers all the input parameters for this component.
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new Param_Model(), "Model", "Model", "Model to convert", GH_ParamAccess.item);
            pManager.AddTextParameter("Path", "Path", "Save the .dat file to this path", GH_ParamAccess.item, @"");
        }

        // Registers all the output parameters for this component.
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.Register_StringParam("Output", "Output", "Converted model");
            pManager.Register_StringParam("Status", "Status", "Errors or success messages");
        }

        // This is the method that actually does the work.
        protected override void SolveInstance(IGH_DataAccess DA) {
            // Some variables
            string output = "";                        // The file output
            string status = "Starting component...\n"; // The debug output

            // Several arrays where the data is stored
            List<Material> materials = new List<Material>();
            List<CrossSection> crossSections = new List<CrossSection>();
            List<Node> nodes = new List<Node>();
            List<Beam> beams = new List<Beam>();
            List<Load> loads = new List<Load>();

            try {
                // Load the data from Karamba

                // Retrieve and clone the input model
                GH_Model in_gh_model = null;
                if (!DA.GetData<GH_Model>(0, ref in_gh_model)) return;
                Model model = in_gh_model.Value;
                model = (Karamba.Models.Model) model.Clone(); //If the model is not cloned a modification to this variable will imply modification of the input model, thus modifying behavior in other components.
                
                string path = null;
                if (!DA.GetData<string>(1, ref path)) { path = ""; }
                if (path == "") {
                    status += "No file path specified. Will not save data to a .dat file.\n";
                }
                
                // Retrieve and store the data
                foreach(Karamba.Materials.FemMaterial material in model.materials) {
                    materials.Add(new Material(material));
                }
                status += materials.Count + " materials loaded...\n";

                foreach (Karamba.CrossSections.CroSec crosec in model.crosecs) {
                    crossSections.Add(new CrossSection(crosec));
                }
                status += crossSections.Count + " cross sections loaded...\n";

                foreach (Karamba.Nodes.Node node in model.nodes) {
                    nodes.Add(new Node(node));
                }
                status += nodes.Count + " nodes loaded...\n";

                foreach (Karamba.Supports.Support support in model.supports) {
                    nodes[support.node_ind].addConstraint(support);
                }
                status += "Support constraints added to " + model.supports.Count + " nodes.\n";

                foreach (Karamba.Elements.ModelElement beam in model.elems) {
                    Beam curBeam = new Beam(beam);

                    // Adding the start and end nodes
                    curBeam.start = nodes[curBeam.ids[0]];
                    curBeam.end = nodes[curBeam.ids[1]];
                    beams.Add(curBeam);
                }
                status += beams.Count + " beams loaded...\n";
                

                // Write the data into a .dat file format
                Parser parser = new Parser(materials, crossSections, nodes, beams, loads);
                output = parser.file;

                if (path != "") {
                    status += "Saving file to " + path + "\n";
                    System.IO.File.WriteAllText(@path, output);
                    status += "File saved!\n";
                }
            }
            catch (Exception e) {
                status += "\nERROR!\n" + e.ToString() + "\n";
            }

            // Return data
            DA.SetData(0, output);
            DA.SetData(1, status);
        }

        // Icon
        protected override System.Drawing.Bitmap Icon {
            get { return Resource.Icon; }
        }

        // Each component must have a unique Guid to identify it. 
        public override Guid ComponentGuid {
            get { return new Guid("{1954a147-f7a2-4d9c-b150-b788821ccae7}"); }
        }
    }
}