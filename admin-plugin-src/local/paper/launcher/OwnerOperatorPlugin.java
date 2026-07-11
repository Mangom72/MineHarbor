package local.paper.launcher;

import java.io.IOException;
import java.io.Reader;
import java.io.Writer;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.util.Properties;
import java.util.UUID;
import org.bukkit.Bukkit;
import org.bukkit.entity.Player;
import org.bukkit.event.EventHandler;
import org.bukkit.event.Listener;
import org.bukkit.event.player.PlayerJoinEvent;
import org.bukkit.plugin.java.JavaPlugin;

public final class OwnerOperatorPlugin extends JavaPlugin implements Listener {
    private String ownerName;
    private UUID ownerUuid;

    @Override
    public void onEnable() {
        loadOwner();
        getServer().getPluginManager().registerEvents(this, this);
        if (!Bukkit.getOnlineMode()) {
            getLogger().warning("온라인 인증이 꺼져 있어 이름 사칭을 막기 위해 자동 OP를 비활성화했습니다.");
            return;
        }
        for (Player player : Bukkit.getOnlinePlayers()) {
            grantOperatorIfOwner(player);
        }
    }

    @EventHandler
    public void onPlayerJoin(PlayerJoinEvent event) {
        if (Bukkit.getOnlineMode()) {
            grantOperatorIfOwner(event.getPlayer());
        }
    }

    private void loadOwner() {
        Properties properties = new Properties();
        Path path = getDataFolder().toPath().resolve("owner.properties");
        try {
            Files.createDirectories(path.getParent());
            if (Files.exists(path)) {
                try (Reader reader = Files.newBufferedReader(path, StandardCharsets.UTF_8)) {
                    properties.load(reader);
                }
            }
        } catch (IOException exception) {
            getLogger().warning("서버 소유자 설정을 읽지 못했습니다: " + exception.getMessage());
        }
        ownerName = properties.getProperty("owner-name", "").trim();
        String uuidText = properties.getProperty("owner-uuid", "").trim();
        if (!uuidText.isEmpty()) {
            try {
                ownerUuid = UUID.fromString(uuidText);
            } catch (IllegalArgumentException exception) {
                getLogger().warning("저장된 서버 소유자 UUID 형식이 올바르지 않습니다.");
            }
        }
    }

    private void grantOperatorIfOwner(Player player) {
        if (ownerUuid != null) {
            if (ownerUuid.equals(player.getUniqueId()) && !player.isOp()) {
                player.setOp(true);
            }
            return;
        }
        if (ownerName.isEmpty() || !ownerName.equalsIgnoreCase(player.getName())) {
            return;
        }
        ownerUuid = player.getUniqueId();
        saveOwnerUuid();
        if (!player.isOp()) {
            player.setOp(true);
        }
    }

    private void saveOwnerUuid() {
        Path path = getDataFolder().toPath().resolve("owner.properties");
        Path temporaryPath = path.resolveSibling("owner.properties.준비중");
        Properties properties = new Properties();
        properties.setProperty("owner-name", ownerName);
        properties.setProperty("owner-uuid", ownerUuid.toString());
        try {
            Files.createDirectories(path.getParent());
            try (Writer writer = Files.newBufferedWriter(temporaryPath, StandardCharsets.UTF_8)) {
                properties.store(writer, "Paper 26.2 서버 런처 소유자 설정");
            }
            try {
                Files.move(temporaryPath, path, StandardCopyOption.REPLACE_EXISTING, StandardCopyOption.ATOMIC_MOVE);
            } catch (IOException exception) {
                Files.move(temporaryPath, path, StandardCopyOption.REPLACE_EXISTING);
            }
        } catch (IOException exception) {
            getLogger().warning("서버 소유자 UUID를 저장하지 못했습니다: " + exception.getMessage());
        } finally {
            try {
                Files.deleteIfExists(temporaryPath);
            } catch (IOException ignored) {
                // 임시 파일 삭제 실패는 다음 저장 시 다시 처리됩니다.
            }
        }
    }
}
