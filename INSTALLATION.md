# Installation Guide

This guide will help you set up the SoccerTwo Unity environment for training and inference.

## Prerequisites

1. **Unity 6000.0 or later**
   - Download from [Unity Download](https://unity.com/download)
   - Install via Unity Hub (recommended)

2. **ML-Agents Unity Package** (v4.0.0 or later)
   - You'll install this in Step 2 below

3. **Python 3.10.1 - 3.10.12** (for training only)
   - Download from [Python.org](https://www.python.org/downloads/)
   - Recommended: Python 3.10.12

---

## Quick Start

### 1. Open Project in Unity

1. Open **Unity Hub**
2. Click **Add** or **Open**
3. Select the `SoccerTwo-Standalone` folder
4. Unity will open the project (first time may take a few minutes to import assets)

### 2. Install ML-Agents Package

After Unity opens, install the ML-Agents package:

#### Option A: From Git URL (Easiest)

1. In Unity, go to `Window > Package Manager`
2. Click the `+` button in the top-left
3. Select `Add package from git URL...`
4. Enter this URL:
   ```
   https://github.com/Unity-Technologies/ml-agents.git?path=/com.unity.ml-agents
   ```
5. Click `Add`
6. Wait for the package to download and import (may take a few minutes)

#### Option B: From Local Folder

If you have the ML-Agents repository cloned locally:

1. Clone the repository:
   ```bash
   git clone https://github.com/Unity-Technologies/ml-agents.git
   ```
2. In Unity, go to `Window > Package Manager`
3. Click the `+` button > `Add package from disk...`
4. Navigate to: `ml-agents/com.unity.ml-agents/package.json`
5. Click `Open`

#### Option C: From Unity Package File

1. Download ML-Agents from [GitHub Releases](https://github.com/Unity-Technologies/ml-agents/releases)
2. Extract the package
3. Follow Option B steps 2-5

### 3. Verify Installation

1. Check for errors in the Console window (`Window > General > Console`)
2. Open the scene: `Assets/SoccerTwo/Scenes/SoccerTwos.unity`
3. If you see any "Missing Script" errors, ensure ML-Agents package is properly installed

---

## Setting Up Python Environment (For Training)

If you want to train agents, you'll need Python and ML-Agents packages:

### 1. Install Python

Download and install Python 3.10.12 from [Python.org](https://www.python.org/downloads/)

**Important**: ML-Agents requires Python 3.10.1 - 3.10.12 (not 3.11+)

### 2. Install ML-Agents Python Packages

Open a terminal/command prompt and run:

```bash
pip install mlagents
```

This will install both `mlagents` and `mlagents-envs` packages.

### 3. Verify Python Installation

```bash
mlagents-learn --help
```

If this shows help text, installation is successful!

---

## Training Your First Model

1. **Ensure Unity project is running** (press Play in Unity, or build executable)

2. **Start training** in terminal:
   ```bash
   mlagents-learn config/sac/SoccerTwo.yaml --run-id=my_first_run
   ```
   Or for POCA:
   ```bash
   mlagents-learn config/poca/SoccerTwos.yaml --run-id=my_poca_run
   ```

3. **Training will start** - you'll see progress in both Unity (agents moving) and terminal (training metrics)

4. **Monitor training** with TensorBoard:
   ```bash
   tensorboard --logdir=results
   ```

---

## Common Issues

### Issue: "Package Manager: Unable to add package"

**Solution**:
- Check your internet connection
- Verify the Git URL is correct
- Try Option B or C instead

### Issue: "Missing Script" errors in Unity

**Solution**:
1. Ensure ML-Agents package is installed
2. Right-click `Assets/SoccerTwo/Scripts` folder > `Reimport All`
3. Restart Unity

### Issue: Scene won't load

**Solution**:
1. Check Console for specific errors
2. Verify all prefabs are present in `Assets/SoccerTwo/Prefabs/`
3. Ensure tags are set: `ball`, `blueGoal`, `purpleGoal`

### Issue: Python version incompatible

**Solution**:
- ML-Agents requires Python 3.10.1 - 3.10.12
- Use `pyenv` or `conda` to install correct version:
  ```bash
  conda create -n mlagents python=3.10.12
  conda activate mlagents
  pip install mlagents
  ```

---

## Next Steps

- Read `README.md` for project structure and features
- Check `Assets/SoccerTwo/Scripts/README_Integration.md` for code details
- Start training with configuration files in `config/` directory

---

## Getting Help

- [ML-Agents Documentation](https://github.com/Unity-Technologies/ml-agents/tree/main/docs)
- [ML-Agents GitHub Issues](https://github.com/Unity-Technologies/ml-agents/issues)
- Check Unity Console for error messages


