import os
import shutil
import glob
dir_path = os.path.dirname(os.path.realpath(__file__))

droneTypes = ['CombatDrone.cs', 'DrillingDrone.cs', 'MothershipDrone.cs', 'ProjectorDrone.cs']


for droneTypeFileName in droneTypes:
    path = dir_path + "\compiled_" + droneTypeFileName
    with open(path, 'wb') as outfile:
        for filename in glob.glob('*/*.cs', recursive=True):
            if filename == path:
                continue
            if os.path.basename(filename) in droneTypes and os.path.basename(filename) != droneTypeFileName:
                continue
            with open(filename, 'rb') as readfile:
                shutil.copyfileobj(readfile, outfile)
