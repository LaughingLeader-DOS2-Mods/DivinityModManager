import os
import sys
from pathlib import Path
import shutil
import zipfile
from zipfile import ZipFile

import contextlib

def get_arg(arg, fallback):
	if len(sys.argv) > arg:
		val = sys.argv[arg]
		if val != None:
			return val
	return fallback

script_dir = Path(os.path.dirname(os.path.abspath(__file__)))
os.chdir(script_dir)

version = get_arg(1, None)

file_name = "DivinityModManager_v{}.zip".format(version)
export_file = "DivinityModManager_Latest.zip"
print("Writing release zip:{}".format(file_name))

def zipdir(src, zip_name):
    ziph = zipfile.ZipFile(zip_name, 'w')
    for root, dirs, files in os.walk(src):
        for file in files:
            ziph.write(os.path.join(root, file), arcname=os.path.join(root.replace(src, ""), file), compress_type=zipfile.ZIP_DEFLATED)
    ziph.close()

def SilentRemove(f):
	try:
		if Path(f).is_dir():
			shutil.rmtree(f, ignore_errors=True)
		else:
			os.remove(f)
		print("Removed {}".format(f))
	except Exception as e:
		print(e)

def SilentCopyAndRemove(source, dest):
	with contextlib.suppress(FileNotFoundError, PermissionError):
		shutil.copy(source, dest)
		if Path(source).is_dir():
			shutil.rmtree(source, ignore_errors=True)
		else:
			os.remove(source)
		print("Removed {}".format(source))

import time
time.sleep(3)

SilentRemove("bin/Publish/Data")
SilentRemove("bin/Publish/_Logs")
SilentRemove(file_name)

zipdir("bin/Publish", file_name)

shutil.copy(file_name, export_file)