#!/bin/bash

set -e

ADDON_NAME=KspTrigger

# Default value
KSP_ROOT="$HOME/.local/share/Steam/steamapps/common/Kerbal Space Program"
COMPIL="true"
DEPLOY="false"

BUILDS_DIR=builds

usage()
{
  echo "Usage: $0 [options]"
  echo "  -k: KSP root path. Default=\"$HOME/.local/share/Steam/steamapps/common/Kerbal Space Program\""
  echo "  -d: compile and deploy"
  echo "  -D: deploy current version"
}

compile()
{
  # Clean
  PROD_DIR=$BUILDS_DIR/$ADDON_NAME
  ADDON_LIB_PATH=$PROD_DIR/Plugins

  if [ -d "$PROD_DIR" ]
  then
    rm -r $PROD_DIR 2>/dev/null
  fi
  mkdir -p $ADDON_LIB_PATH

  # Production
  SRC_DIR="src"
  SRC_SUBDIRS="action condition event trigger ui utils"
  SRC_FILES=""
  for subdir in $SRC_SUBDIRS
  do
    SRC_FILES=${SRC_FILES:+$SRC_FILES }$SRC_DIR/$subdir/*.cs
  done

  ADDON_LIB=$ADDON_NAME.dll
  KSP_LIB=$KSP_ROOT/KSP_Data/Managed

  mcs -lib:"$KSP_LIB" -r:Assembly-CSharp.dll,Assembly-CSharp-firstpass.dll,UnityEngine.dll,UnityEngine.CoreModule.dll,UnityEngine.UI.dll,UnityEngine.AnimationModule.dll,UnityEngine.ImageConversionModule.dll,UnityEngine.IMGUIModule.dll,UnityEngine.TextRenderingModule.dll -target:library -out:"$ADDON_LIB_PATH/$ADDON_LIB" $SRC_FILES

  # Archive
  RESOURCES_DIR=Resources
  RESOURCES_FILES=$RESOURCES_DIR/*
  cp $RESOURCES_FILES $PROD_DIR

  (
    cd $BUILDS_DIR
    if [ -f $ADDON_NAME.zip ]
    then
      rm $ADDON_NAME.zip
    fi
    zip -qq $ADDON_NAME.zip -r $ADDON_NAME
  )
}

deploy()
{
  # Deployment
  unzip -o -qq $BUILDS_DIR/$ADDON_NAME.zip -d "$KSP_ROOT/GameData"
}

cd "$(dirname "$0")"

# Option
while getopts ":kdDh" option; do
  case "${option}" in
    k)
      KSP_ROOT=${OPTARG}
      ;;
    d)
      DEPLOY="true"
      ;;
    D)
      COMPIL="false"
      DEPLOY="true"
      ;;
    h)
      usage
      exit 0
      ;;
    *)
      usage
      exit 1
      ;;
  esac
done

if [ "$COMPIL" == "true" ]
then
  compile
fi

if [ "$DEPLOY" == "true" ]
then
  deploy
fi
