﻿using MassSpectrometry;
using MzLibUtil;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;

namespace IO.MzML
{
    public static class MzmlMethods
    {

        #region Internal Fields

        internal static readonly XmlSerializer indexedSerializer = new XmlSerializer(typeof(Generated.indexedmzML));
        internal static readonly XmlSerializer mzmlSerializer = new XmlSerializer(typeof(Generated.mzMLType));

        #endregion Internal Fields

        #region Private Fields

        private static readonly Dictionary<DissociationType, string> DissociationTypeAccessions = new Dictionary<DissociationType, string>{
            {DissociationType.HCD, "MS:1000422"},
            {DissociationType.CID, "MS:1000133"},
            {DissociationType.Unknown, "MS:1000044"}};

        private static readonly Dictionary<DissociationType, string> DissociationTypeNames = new Dictionary<DissociationType, string>{
            {DissociationType.HCD, "beam-type collision-induced dissociation"},
            {DissociationType.CID, "collision-induced dissociation"},
            {DissociationType.Unknown, "dissociation method"}};

        private static readonly Dictionary<bool, string> CentroidAccessions = new Dictionary<bool, string>{
            {true, "MS:1000127"},
            {false, "MS:1000128"}};

        private static readonly Dictionary<bool, string> CentroidNames = new Dictionary<bool, string>{
            {true, "centroid spectrum"},
            {false, "profile spectrum"}};

        private static readonly Dictionary<Polarity, string> PolarityAccessions = new Dictionary<Polarity, string>{
            {Polarity.Negative, "MS:1000129"},
            {Polarity.Positive, "MS:1000130"}};

        private static readonly Dictionary<Polarity, string> PolarityNames = new Dictionary<Polarity, string>{
            {Polarity.Negative, "negative scan"},
            {Polarity.Positive, "positive scan"}};
        #endregion Private Fields

        #region Public Methods

        public static void CreateAndWriteMyMzmlWithCalibratedSpectra(IMsDataFile<IMsDataScan<IMzSpectrum<IMzPeak>>> myMsDataFile, string outputFile)
        {
            var mzML = new Generated.mzMLType();
            //mzML.version = "1";

            mzML.cvList = new Generated.CVListType();
            mzML.cvList.count = "1";
            mzML.cvList.cv = new Generated.CVType[1];
            mzML.cvList.cv[0] = new Generated.CVType();
            mzML.cvList.cv[0].URI = @"https://raw.githubusercontent.com/HUPO-PSI/psi-ms-CV/master/psi-ms.obo";
            mzML.cvList.cv[0].fullName = "Proteomics Standards Initiative Mass Spectrometry Ontology";
            mzML.cvList.cv[0].id = "MS";

            mzML.fileDescription = new Generated.FileDescriptionType();
            mzML.fileDescription.fileContent = new Generated.ParamGroupType();
            mzML.fileDescription.fileContent.cvParam = new Generated.CVParamType[2];
            mzML.fileDescription.fileContent.cvParam[0] = new Generated.CVParamType();
            mzML.fileDescription.fileContent.cvParam[0].accession = "MS:1000579"; // MS1 Data
            mzML.fileDescription.fileContent.cvParam[1] = new Generated.CVParamType();
            mzML.fileDescription.fileContent.cvParam[1].accession = "MS:1000580"; // MSn Data

            mzML.softwareList = new Generated.SoftwareListType();
            mzML.softwareList.count = "1";

            mzML.softwareList.software = new Generated.SoftwareType[1];

            // TODO: add the raw file fields
            mzML.softwareList.software[0] = new Generated.SoftwareType();
            mzML.softwareList.software[0].id = "mzLib";
            mzML.softwareList.software[0].version = "1";
            mzML.softwareList.software[0].cvParam = new Generated.CVParamType[1];
            mzML.softwareList.software[0].cvParam[0] = new Generated.CVParamType();
            mzML.softwareList.software[0].cvParam[0].accession = "MS:1000799";
            mzML.softwareList.software[0].cvParam[0].value = "mzLib";

            // Leaving empty. Can't figure out the configurations.
            // ToDo: read instrumentConfigurationList from mzML file
            mzML.instrumentConfigurationList = new Generated.InstrumentConfigurationListType();

            mzML.dataProcessingList = new Generated.DataProcessingListType();
            // Only writing mine! Might have had some other data processing (but not if it is a raw file)
            // ToDo: read dataProcessingList from mzML file
            mzML.dataProcessingList.count = "1";
            mzML.dataProcessingList.dataProcessing = new Generated.DataProcessingType[1];
            mzML.dataProcessingList.dataProcessing[0] = new Generated.DataProcessingType();
            mzML.dataProcessingList.dataProcessing[0].id = "mzLibProcessing";

            mzML.run = new Generated.RunType();

            // ToDo: Finish the chromatogram writing!
            mzML.run.chromatogramList = new Generated.ChromatogramListType();
            mzML.run.chromatogramList.count = "1";
            mzML.run.chromatogramList.chromatogram = new Generated.ChromatogramType[1];
            mzML.run.chromatogramList.chromatogram[0] = new Generated.ChromatogramType();

            mzML.run.spectrumList = new Generated.SpectrumListType();
            mzML.run.spectrumList.count = (myMsDataFile.NumSpectra).ToString(CultureInfo.InvariantCulture);
            mzML.run.spectrumList.defaultDataProcessingRef = "mzLibProcessing";
            mzML.run.spectrumList.spectrum = new Generated.SpectrumType[myMsDataFile.NumSpectra];

            // Loop over all spectra
            for (int i = 1; i <= myMsDataFile.NumSpectra; i++)
            {
                mzML.run.spectrumList.spectrum[i - 1] = new Generated.SpectrumType();

                mzML.run.spectrumList.spectrum[i - 1].defaultArrayLength = myMsDataFile.GetOneBasedScan(i).MassSpectrum.Size;

                mzML.run.spectrumList.spectrum[i - 1].index = i.ToString(CultureInfo.InvariantCulture);
                mzML.run.spectrumList.spectrum[i - 1].id = myMsDataFile.GetOneBasedScan(i).OneBasedScanNumber.ToString();

                mzML.run.spectrumList.spectrum[i - 1].cvParam = new Generated.CVParamType[8];

                mzML.run.spectrumList.spectrum[i - 1].cvParam[0] = new Generated.CVParamType();

                if (myMsDataFile.GetOneBasedScan(i).MsnOrder == 1)
                {
                    mzML.run.spectrumList.spectrum[i - 1].cvParam[0].accession = "MS:1000579";
                }
                else if (myMsDataFile.GetOneBasedScan(i) is IMsDataScanWithPrecursor<IMzSpectrum<IMzPeak>>)
                {
                    var scanWithPrecursor = myMsDataFile.GetOneBasedScan(i) as IMsDataScanWithPrecursor<IMzSpectrum<IMzPeak>>;
                    mzML.run.spectrumList.spectrum[i - 1].cvParam[0].accession = "MS:1000580";

                    // So needs a precursor!
                    mzML.run.spectrumList.spectrum[i - 1].precursorList = new Generated.PrecursorListType();
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.count = 1.ToString();
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor = new Generated.PrecursorType[1];
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0] = new Generated.PrecursorType();
                    string precursorID = scanWithPrecursor.OneBasedPrecursorScanNumber.ToString();
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].spectrumRef = precursorID;
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList = new Generated.SelectedIonListType();
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.count = 1.ToString();
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon = new Generated.ParamGroupType[1];
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0] = new Generated.ParamGroupType();
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam = new Generated.CVParamType[3];
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0] = new Generated.CVParamType();
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0].name = "selected ion m/z";

                    // Selected ion MZ
                    if (scanWithPrecursor.SelectedIonGuessMonoisotopicMZ.HasValue)
                    {
                        mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0] = new Generated.CVParamType();
                        mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0].name = "selected ion m/z";
                        mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0].value = scanWithPrecursor.SelectedIonGuessMonoisotopicMZ.Value.ToString(CultureInfo.InvariantCulture);
                        mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0].accession = "MS:1000744";
                    }

                    // Charge State
                    if (scanWithPrecursor.SelectedIonGuessChargeStateGuess.HasValue)
                    {
                        mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1] = new Generated.CVParamType();
                        mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1].name = "charge state";
                        mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1].value = scanWithPrecursor.SelectedIonGuessChargeStateGuess.Value.ToString(CultureInfo.InvariantCulture);
                        mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1].accession = "MS:1000041";
                    }

                    // Selected ion intensity
                    if (scanWithPrecursor.SelectedIonGuessIntensity.HasValue)
                    {
                        mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2] = new Generated.CVParamType();
                        mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2].name = "peak intensity";
                        double selectedIonGuesssMonoisotopicIntensity = scanWithPrecursor.SelectedIonGuessIntensity.Value;
                        mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2].value = selectedIonGuesssMonoisotopicIntensity.ToString(CultureInfo.InvariantCulture);
                        mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2].accession = "MS:1000042";
                    }

                    MzRange isolationRange = scanWithPrecursor.IsolationRange;
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow = new Generated.ParamGroupType();
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam = new Generated.CVParamType[3];
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[0] = new Generated.CVParamType();
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[0].accession = "MS:1000827";
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[0].name = "isolation window target m/z";
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[0].value = isolationRange.Mean.ToString(CultureInfo.InvariantCulture);
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[1] = new Generated.CVParamType();
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[1].accession = "MS:1000828";
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[1].name = "isolation window lower offset";
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[1].value = (isolationRange.Width / 2).ToString(CultureInfo.InvariantCulture);
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[2] = new Generated.CVParamType();
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[2].accession = "MS:1000829";
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[2].name = "isolation window upper offset";
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[2].value = (isolationRange.Width / 2).ToString(CultureInfo.InvariantCulture);

                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].activation = new Generated.ParamGroupType();
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].activation.cvParam = new Generated.CVParamType[1];
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].activation.cvParam[0] = new Generated.CVParamType();

                    DissociationType dissociationType = scanWithPrecursor.DissociationType;

                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].activation.cvParam[0].accession = DissociationTypeAccessions[dissociationType];
                    mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].activation.cvParam[0].name = DissociationTypeNames[dissociationType];
                }

                mzML.run.spectrumList.spectrum[i - 1].cvParam[1] = new Generated.CVParamType();
                mzML.run.spectrumList.spectrum[i - 1].cvParam[1].name = "ms level";
                mzML.run.spectrumList.spectrum[i - 1].cvParam[1].accession = "MS:1000511";
                mzML.run.spectrumList.spectrum[i - 1].cvParam[1].value = myMsDataFile.GetOneBasedScan(i).MsnOrder.ToString(CultureInfo.InvariantCulture);

                mzML.run.spectrumList.spectrum[i - 1].cvParam[2] = new Generated.CVParamType();
                mzML.run.spectrumList.spectrum[i - 1].cvParam[2].name = CentroidNames[myMsDataFile.GetOneBasedScan(i).IsCentroid];
                mzML.run.spectrumList.spectrum[i - 1].cvParam[2].accession = CentroidAccessions[myMsDataFile.GetOneBasedScan(i).IsCentroid];

                string polarityName;
                string polarityAccession;
                if (PolarityNames.TryGetValue(myMsDataFile.GetOneBasedScan(i).Polarity, out polarityName) && PolarityAccessions.TryGetValue(myMsDataFile.GetOneBasedScan(i).Polarity, out polarityAccession))
                {
                    mzML.run.spectrumList.spectrum[i - 1].cvParam[3] = new Generated.CVParamType();
                    mzML.run.spectrumList.spectrum[i - 1].cvParam[3].name = polarityName;
                    mzML.run.spectrumList.spectrum[i - 1].cvParam[3].accession = polarityAccession;
                }
                // Spectrum title
                mzML.run.spectrumList.spectrum[i - 1].cvParam[4] = new Generated.CVParamType();
                mzML.run.spectrumList.spectrum[i - 1].cvParam[4].name = "spectrum title";
                mzML.run.spectrumList.spectrum[i - 1].cvParam[4].accession = "MS:1000796";
                mzML.run.spectrumList.spectrum[i - 1].cvParam[4].value = myMsDataFile.GetOneBasedScan(i).OneBasedScanNumber.ToString();

                if ((myMsDataFile.GetOneBasedScan(i).MassSpectrum.Size) > 0)
                {
                    // Lowest observed mz
                    mzML.run.spectrumList.spectrum[i - 1].cvParam[5] = new Generated.CVParamType();
                    mzML.run.spectrumList.spectrum[i - 1].cvParam[5].name = "lowest observed m/z";
                    mzML.run.spectrumList.spectrum[i - 1].cvParam[5].accession = "MS:1000528";
                    mzML.run.spectrumList.spectrum[i - 1].cvParam[5].value = myMsDataFile.GetOneBasedScan(i).MassSpectrum.FirstX.ToString(CultureInfo.InvariantCulture);

                    // Highest observed mz
                    mzML.run.spectrumList.spectrum[i - 1].cvParam[6] = new Generated.CVParamType();
                    mzML.run.spectrumList.spectrum[i - 1].cvParam[6].name = "highest observed m/z";
                    mzML.run.spectrumList.spectrum[i - 1].cvParam[6].accession = "MS:1000527";
                    mzML.run.spectrumList.spectrum[i - 1].cvParam[6].value = myMsDataFile.GetOneBasedScan(i).MassSpectrum.LastX.ToString(CultureInfo.InvariantCulture);
                }

                // Total ion current
                mzML.run.spectrumList.spectrum[i - 1].cvParam[7] = new Generated.CVParamType();
                mzML.run.spectrumList.spectrum[i - 1].cvParam[7].name = "total ion current";
                mzML.run.spectrumList.spectrum[i - 1].cvParam[7].accession = "MS:1000285";
                mzML.run.spectrumList.spectrum[i - 1].cvParam[7].value = myMsDataFile.GetOneBasedScan(i).TotalIonCurrent.ToString(CultureInfo.InvariantCulture);

                // Retention time
                mzML.run.spectrumList.spectrum[i - 1].scanList = new Generated.ScanListType();
                mzML.run.spectrumList.spectrum[i - 1].scanList.count = "1";
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan = new Generated.ScanType[1];
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0] = new Generated.ScanType();
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam = new Generated.CVParamType[3];
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[0] = new Generated.CVParamType();
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[0].name = "scan start time";
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[0].accession = "MS:1000016";
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[0].value = myMsDataFile.GetOneBasedScan(i).RetentionTime.ToString(CultureInfo.InvariantCulture);
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[0].unitCvRef = "UO";
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[0].unitAccession = "UO:0000031";
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[0].unitName = "minute";
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[1] = new Generated.CVParamType();
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[1].name = "filter string";
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[1].accession = "MS:1000512";
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[1].value = myMsDataFile.GetOneBasedScan(i).ScanFilter;
                if (myMsDataFile.GetOneBasedScan(i).InjectionTime.HasValue)
                {
                    mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[2] = new Generated.CVParamType();
                    mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[2].name = "ion injection time";
                    mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[2].accession = "MS:1000927";
                    mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[2].value = myMsDataFile.GetOneBasedScan(i).InjectionTime.Value.ToString(CultureInfo.InvariantCulture);
                }
                if (myMsDataFile.GetOneBasedScan(i) is IMsDataScanWithPrecursor<IMzSpectrum<IMzPeak>>)
                {
                    var scanWithPrecursor = myMsDataFile.GetOneBasedScan(i) as IMsDataScanWithPrecursor<IMzSpectrum<IMzPeak>>;
                    if (scanWithPrecursor.SelectedIonGuessMonoisotopicMZ.HasValue)
                    {
                        mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].userParam = new Generated.UserParamType[1];
                        mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].userParam[0] = new Generated.UserParamType();
                        mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].userParam[0].name = "[mzLib]Monoisotopic M/Z:";
                        mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].userParam[0].value = scanWithPrecursor.SelectedIonGuessMonoisotopicMZ.Value.ToString(CultureInfo.InvariantCulture);
                    }
                }

                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList = new Generated.ScanWindowListType();
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.count = 1;
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow = new Generated.ParamGroupType[1];
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0] = new Generated.ParamGroupType();
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0].cvParam = new Generated.CVParamType[2];
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0].cvParam[0] = new Generated.CVParamType();
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0].cvParam[0].name = "scan window lower limit";
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0].cvParam[0].accession = "MS:1000501";
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0].cvParam[0].value = myMsDataFile.GetOneBasedScan(i).ScanWindowRange.Minimum.ToString(CultureInfo.InvariantCulture);
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0].cvParam[1] = new Generated.CVParamType();
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0].cvParam[1].name = "scan window upper limit";
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0].cvParam[1].accession = "MS:1000500";
                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0].cvParam[1].value = myMsDataFile.GetOneBasedScan(i).ScanWindowRange.Maximum.ToString(CultureInfo.InvariantCulture);

                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList = new Generated.BinaryDataArrayListType();

                // ONLY WRITING M/Z AND INTENSITY DATA, NOT THE CHARGE! (but can add charge info later)
                // CHARGE (and other stuff) CAN BE IMPORTANT IN ML APPLICATIONS!!!!!
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.count = 2.ToString();
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray = new Generated.BinaryDataArrayType[5];

                // M/Z Data
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0] = new Generated.BinaryDataArrayType();
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].binary = myMsDataFile.GetOneBasedScan(i).MassSpectrum.Get64BitXarray();
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].encodedLength = (4 * Math.Ceiling(((double)mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].binary.Length / 3))).ToString(CultureInfo.InvariantCulture);
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam = new Generated.CVParamType[3];
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam[0] = new Generated.CVParamType();
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam[0].accession = "MS:1000514";
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam[0].name = "m/z array";
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam[1] = new Generated.CVParamType();
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam[1].accession = "MS:1000523";
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam[1].name = "64-bit float";
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam[2] = new Generated.CVParamType();
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam[2].accession = "MS:1000576";
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam[2].name = "no compression";

                // Intensity Data
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1] = new Generated.BinaryDataArrayType();
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].binary = myMsDataFile.GetOneBasedScan(i).MassSpectrum.Get64BitYarray();
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].encodedLength = (4 * Math.Ceiling(((double)mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].binary.Length / 3))).ToString(CultureInfo.InvariantCulture);
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam = new Generated.CVParamType[3];
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam[0] = new Generated.CVParamType();
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam[0].accession = "MS:1000515";
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam[0].name = "intensity array";
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam[1] = new Generated.CVParamType();
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam[1].accession = "MS:1000523";
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam[1].name = "64-bit float";
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam[2] = new Generated.CVParamType();
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam[2].accession = "MS:1000576";
                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam[2].name = "no compression";

                if (myMsDataFile.GetOneBasedScan(i).NoiseData != null)
                {
                    // mass
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2] = new Generated.BinaryDataArrayType();
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].binary = myMsDataFile.GetOneBasedScan(i).Get64BitNoiseDataMass();
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].encodedLength = (4 * Math.Ceiling(((double)mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].binary.Length / 3))).ToString(CultureInfo.InvariantCulture);
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam = new Generated.CVParamType[3];
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam[0] = new Generated.CVParamType();
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam[0].accession = "MS:1000786";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam[0].name = "non-standard data array";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam[1] = new Generated.CVParamType();
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam[1].accession = "MS:1000523";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam[1].name = "64-bit float";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam[2] = new Generated.CVParamType();
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam[2].accession = "MS:1000576";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam[2].name = "no compression";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].userParam = new Generated.UserParamType[1];
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].userParam[0] = new Generated.UserParamType();
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].userParam[0].name = "kelleherCustomType";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].userParam[0].value = "noise m/z";

                    // noise
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3] = new Generated.BinaryDataArrayType();
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].binary = myMsDataFile.GetOneBasedScan(i).Get64BitNoiseDataNoise();
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].encodedLength = (4 * Math.Ceiling(((double)mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].binary.Length / 3))).ToString(CultureInfo.InvariantCulture);
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam = new Generated.CVParamType[3];
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam[0] = new Generated.CVParamType();
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam[0].accession = "MS:1000786";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam[0].name = "non-standard data array";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam[1] = new Generated.CVParamType();
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam[1].accession = "MS:1000523";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam[1].name = "64-bit float";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam[2] = new Generated.CVParamType();
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam[2].accession = "MS:1000576";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam[2].name = "no compression";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].userParam = new Generated.UserParamType[1];
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].userParam[0] = new Generated.UserParamType();
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].userParam[0].name = "kelleherCustomType";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].userParam[0].value = "noise baseline";

                    // baseline
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4] = new Generated.BinaryDataArrayType();
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].binary = myMsDataFile.GetOneBasedScan(i).Get64BitNoiseDataBaseline();
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].encodedLength = (4 * Math.Ceiling(((double)mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].binary.Length / 3))).ToString(CultureInfo.InvariantCulture);
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam = new Generated.CVParamType[3];
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam[0] = new Generated.CVParamType();
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam[0].accession = "MS:1000786";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam[0].name = "non-standard data array";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam[1] = new Generated.CVParamType();
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam[1].accession = "MS:1000523";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam[1].name = "64-bit float";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam[2] = new Generated.CVParamType();
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam[2].accession = "MS:1000576";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam[2].name = "no compression";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].userParam = new Generated.UserParamType[1];
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].userParam[0] = new Generated.UserParamType();
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].userParam[0].name = "kelleherCustomType";
                    mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].userParam[0].value = "noise intensity";
                }
            }

            Write(outputFile, mzML);
        }

        #endregion Public Methods

        #region Private Methods

        private static void Write(string filePath, Generated.mzMLType _indexedmzMLConnection)
        {
            TextWriter writer = new StreamWriter(filePath);

            mzmlSerializer.Serialize(writer, _indexedmzMLConnection);
            writer.Close();

            //using (TextWriter writer = new StreamWriter(filePath))
            //{
            //    mzmlSerializer.Serialize(writer, _indexedmzMLConnection);
            //}
        }

        #endregion Private Methods

    }
}