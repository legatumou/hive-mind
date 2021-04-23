import os
import shutil
import glob
import re
import string
dir_path = os.path.dirname(os.path.realpath(__file__))
WINDOWS_LINE_ENDING = b'\r\n'
UNIX_LINE_ENDING = b'\n'

droneTypes = ['CombatDrone.cs', 'DrillingDrone.cs', 'MothershipDrone.cs', 'ProjectorDrone.cs', 'PlayerDrone.cs']

for droneTypeFileName in droneTypes:
    path = dir_path + "\compiled_" + droneTypeFileName
    with open(path, 'wb') as outfile:
        for filename in glob.glob('*/*.cs', recursive=True):
            if filename == path:
                continue
            if os.path.basename(filename) in droneTypes and os.path.basename(filename) != droneTypeFileName:
                continue
            with open(filename, 'rb') as readfile:
                data = readfile.read().decode('utf-8')
                data.lstrip()
                re.sub('^\s*', " ", data)
                outfile.write(data.encode('utf-8'))
