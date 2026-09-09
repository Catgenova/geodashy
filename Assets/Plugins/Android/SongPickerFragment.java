package com.catgenova.lyreflyer;

import android.app.Activity;
import android.app.Fragment;
import android.content.Intent;
import android.database.Cursor;
import android.net.Uri;
import android.os.Bundle;
import android.provider.OpenableColumns;

import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.OutputStream;

/**
 * Headless fragment that opens the system document picker for audio and copies the chosen file into
 * targetDir, then writes the copied path (or nothing) into resultFile for the C# side to pick up.
 * Using a fragment means no manifest entry and no custom activity are needed.
 */
public class SongPickerFragment extends Fragment {
    private static final int REQUEST = 4242;
    private String targetDir;
    private String resultFile;

    public static void pick(Activity activity, String targetDir, String resultFile) {
        SongPickerFragment f = new SongPickerFragment();
        f.targetDir = targetDir;
        f.resultFile = resultFile;
        activity.getFragmentManager().beginTransaction().add(f, "lyreflyer_song_picker").commitAllowingStateLoss();
    }

    @Override
    public void onCreate(Bundle saved) {
        super.onCreate(saved);
        Intent intent = new Intent(Intent.ACTION_OPEN_DOCUMENT);
        intent.addCategory(Intent.CATEGORY_OPENABLE);
        intent.setType("audio/*");
        try {
            startActivityForResult(intent, REQUEST);
        } catch (Exception e) {
            finish("");
        }
    }

    @Override
    public void onActivityResult(int requestCode, int resultCode, Intent data) {
        if (requestCode != REQUEST) return;
        String out = "";
        if (resultCode == Activity.RESULT_OK && data != null && data.getData() != null) {
            try {
                out = copy(data.getData());
            } catch (Exception e) {
                out = "";
            }
        }
        finish(out);
    }

    private void finish(String path) {
        try {
            File f = new File(resultFile);
            File parent = f.getParentFile();
            if (parent != null) parent.mkdirs();
            OutputStream o = new FileOutputStream(f);
            o.write(path.getBytes("UTF-8"));
            o.close();
        } catch (Exception ignored) {
        }
        try {
            getActivity().getFragmentManager().beginTransaction().remove(this).commitAllowingStateLoss();
        } catch (Exception ignored) {
        }
    }

    private String copy(Uri uri) throws Exception {
        String name = displayName(uri);
        File dir = new File(targetDir);
        dir.mkdirs();
        File target = new File(dir, name);
        InputStream in = getActivity().getContentResolver().openInputStream(uri);
        if (in == null) return "";
        OutputStream out = new FileOutputStream(target);
        byte[] buf = new byte[65536];
        int n;
        while ((n = in.read(buf)) > 0) out.write(buf, 0, n);
        out.close();
        in.close();
        return target.getAbsolutePath();
    }

    private String displayName(Uri uri) {
        String name = "song.mp3";
        Cursor c = null;
        try {
            c = getActivity().getContentResolver().query(uri, null, null, null, null);
            if (c != null && c.moveToFirst()) {
                int idx = c.getColumnIndex(OpenableColumns.DISPLAY_NAME);
                if (idx >= 0) {
                    String s = c.getString(idx);
                    if (s != null && s.length() > 0) name = s;
                }
            }
        } catch (Exception ignored) {
        } finally {
            if (c != null) c.close();
        }
        return name.replaceAll("[^A-Za-z0-9._ -]", "_");
    }
}
