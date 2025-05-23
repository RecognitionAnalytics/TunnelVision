# -*- coding: utf-8 -*-
"""
Created on Tue Apr 12 13:52:52 2016

@author: bashc
"""
import Experiment


directoryPath= 'S:\\Research\\Stacked Junctions\\Results\\20160229_NALDB34_Chip03_22cyc_HIM3pC_UVozone10each_200deg_1mMPB_pH7_modified\\For Brain analysis\\M1-M2'
e=Experiment.Experiment(directoryPath)
outputTable=e.runStatistics()

with open("c:\\pyStuff\\Stats.csv", "w") as text_file:
    text_file.write(outputTable)