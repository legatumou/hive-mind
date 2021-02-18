import os
import shutil
import glob
dir_path = os.path.dirname(os.path.realpath(__file__))

path = dir_path + "\compiled.cs"
print(dir_path)

with open(path, 'wb') as outfile:
    for filename in glob.glob('*/*.cs', recursive=True):
        if filename == path:
            continue
        with open(filename, 'rb') as readfile:
            shutil.copyfileobj(readfile, outfile)
