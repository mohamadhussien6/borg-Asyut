import tempfile
import unittest
from pathlib import Path
import zipfile
from package_store import package, NOTICES

class StorePackagingTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        for rel in NOTICES.values():
            p = self.root / rel
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_text("fixture", encoding="utf8")

    def test_windows(self):
        src = self.root / "Windows"
        src.mkdir()
        for name in ("Borg Asyut.exe", "UnityPlayer.dll", "debug.pdb"):
            (src / name).write_bytes(b"fixture")
        data = src / "Borg Asyut_Data"
        data.mkdir()
        (data / "globalgamemanagers").write_bytes(b"fixture")
        output = self.root / "Windows.zip"
        package("windows", src, output, self.root)
        with zipfile.ZipFile(output) as z:
            self.assertIn("Borg Asyut.exe", z.namelist())
            self.assertIn("Third_Party_Notices/CREDITS.md", z.namelist())
            self.assertNotIn("debug.pdb", z.namelist())
        self.assertTrue(output.with_suffix(".zip.sha256").is_file())

    def test_web(self):
        src = self.root / "Web"
        build = src / "Build"
        build.mkdir(parents=True)
        (src / "index.html").write_text("fixture")
        for suffix in (".loader.js", ".framework.js.unityweb", ".wasm.unityweb", ".data.unityweb"):
            (build / ("Web" + suffix)).write_bytes(b"fixture")
        output = self.root / "Web.zip"
        package("web", src, output, self.root)
        with zipfile.ZipFile(output) as z:
            self.assertIn("index.html", z.namelist())
            self.assertIn("Build/Web.wasm.unityweb", z.namelist())

    def test_incomplete(self):
        src = self.root / "Broken"
        src.mkdir()
        with self.assertRaises(ValueError):
            package("web", src, self.root / "bad.zip", self.root)
        with self.assertRaises(ValueError):
            package("windows", src, self.root / "bad.zip", self.root)

    def test_rejects_unknown_platform(self):
        src = self.root / "Web"
        build = src / "Build"
        build.mkdir(parents=True)
        (src / "index.html").write_text("fixture")
        for suffix in (".loader.js", ".framework.js", ".wasm", ".data"):
            (build / ("Web" + suffix)).write_bytes(b"fixture")
        with self.assertRaisesRegex(ValueError, "platform"):
            package("android", src, self.root / "bad.zip", self.root)

if __name__ == "__main__":
    unittest.main()
